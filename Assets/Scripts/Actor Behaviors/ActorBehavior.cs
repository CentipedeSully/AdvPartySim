using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Sirenix.OdinInspector;


public enum DamageType
{
    Physical,
    Magical
}

public enum IndicatorType
{
    Miss,
    Damage,
    Heal,
    LevelUp
}

public interface IActor
{
    void HurtActor(int damage, DamageType damageType, Vector2 AttackerPosition);

    void HealActor(int heal);

    int GetID();

    Transform GetTransform();

    void PlaySound(string sound);

    void ShowIndicator(IndicatorType type, string value);

}

public class ActorBehavior : MonoBehaviour, IActor, IQuickLoggable
{
    //Declarations
    [BoxGroup("Combat")]
    [SerializeField]
    [Min(.5f)]
    private float _atkCooldownDuration = .5f;

    [BoxGroup("Combat")]
    [SerializeField] private float _hurtStunDuration;

    [BoxGroup("Combat")]
    [SerializeField] private float _attackWindupDuration;

    [BoxGroup("Combat")]
    [SerializeField] private float _attackRecoveryDuration;

    [BoxGroup("Combat")]
    [Tooltip("100 is gauranteed hit")]
    [SerializeField] private int _hitChance = 80;


    [TabGroup("Combat Tabs", "Combat States")]
    [SerializeField] private bool _isInCombat = false;

    [TabGroup("Combat Tabs", "Combat States")] 
    [SerializeField] private bool _isAtkReady = true;

    [TabGroup("Combat Tabs", "Combat States")]
    [SerializeField] private bool _isAtkCoolingDown = false;

    [TabGroup("Combat Tabs", "Combat States")]
    [SerializeField] private bool _isHurtStunned = false;
    private float _remainingHurtStunTime = 0;


    [TabGroup("Combat Tabs", "Combat Targeting")]
    [SerializeField] private IActor _target;

    [TabGroup("Combat Tabs", "Combat Animation Tweaks")]
    [SerializeField] private Transform _spriteParentTransform;

    [TabGroup("Combat Tabs", "Combat Animation Tweaks")]
    [SerializeField] private float _meleeAtkLerpDistance = .3f;

    [TabGroup("Combat Tabs", "Combat Animation Tweaks")]
    [SerializeField] private float _lerpToTargetDuration;

    [TabGroup("Combat Tabs", "Combat Animation Tweaks")]
    [SerializeField] private float _lerpBackToOriginDuration;

    private Vector2 _atkOrigin;
    private Vector2 _atkDirection = Vector2.zero;
    private bool _isPositionOffset = false;
    private bool _isLerpingTowardsTarget = false;

    private float _currentLerpToTargetTime = 0;
    private Vector2 _toTargetStartPoint;
    private Vector2 _toTargetEndPoint;

    private float _currentLerpToOriginTime = 0;
    private Vector2 _toOriginStartPoint;
    private Vector2 _toOriginEndPoint;

    private IEnumerator _attackCoordinator;


    [Space]
    [TabGroup("Effects","SFX")][SerializeField] private AudioSource _meleeMiss;
    [TabGroup("Effects", "SFX")][SerializeField] private Vector2 _missPitchRange;
    [TabGroup("Effects", "SFX")][SerializeField] private AudioSource _meleeHit;
    [TabGroup("Effects", "SFX")][SerializeField] private Vector2 _hitPitchRange;


    [TabGroup("Effects", "Animaton Parameters")]
    [SerializeField] private Animator _animator;

    [TabGroup("Effects", "Animaton Parameters")]
    [SerializeField] private string _atkParam = "isAttacking";

    [TabGroup("Effects", "Animaton Parameters")]
    [SerializeField] private string _hurtParam = "isHurt";


    [TabGroup("Effects", "UI Graphics")]
    [SerializeField] private CreateIndicator _indicatorCreater;
    [TabGroup("Effects", "UI Graphics")]
    [SerializeField] private Transform _indicatorContainer;


    [Space]
    [Header("Debug Mode")]
    [SerializeField] private bool _isDebugActive = false;
    [ShowIfGroup("_isDebugActive")]



    [TabGroup("_isDebugActive/Debug","Parameters")]
    [SerializeField] private bool _DEBUG_setTarget_cmd = false;

    [TabGroup("_isDebugActive/Debug", "Parameters")]
    [SerializeField] private ActorBehavior _DEBUG_targetActor_param;

    [TabGroup("_isDebugActive/Debug", "Parameters")]
    [SerializeField][DisableIf("@true")] private bool _DEBUG_isTargetSet = false;



    [TabGroup("_isDebugActive/Debug", "Commands")]
    [SerializeField] private bool _DEBUG_spamAttacks_cmd = false;

    [TabGroup("_isDebugActive/Debug", "Commands")]
    [SerializeField] private bool _DEBUG_enterAttack_cmd = false;

    [TabGroup("_isDebugActive/Debug", "Commands")]
    [SerializeField] private bool _DEBUG_exitAttack_cmd = false;

    [TabGroup("_isDebugActive/Debug", "Commands")]
    [SerializeField] private bool _DEBUG_takeDamage_cmd = false;

    //Monobehaviors

    private void Update()
    {
        ListenForDebugCommands();
        TickHurtStun();
        TickAtkLerp();
        
    }


    //Internals
    private void StartNewAttackSequence()
    {
        //Only start an attack if we have a target and our atk is ready
        if (_target != null && _isAtkReady)
        {
            //Raise warning if we're attempting multiple attack simultaneously
            if (_attackCoordinator != null)
            {
                QuickLogger.Warn(this, "Attempted to start a new attack when one was already in progress. Ignoring Request");
                return;
            }

            else
            {
                //unready our atk. Cooling down begins when the atk ends (or is interrupted)
                _isAtkReady = false;

                //save our current position as the atk origin
                _atkOrigin = transform.position;

                //Calculate the direction from ourself to the target
                float xDistanceFromTarget = _target.GetTransform().position.x - transform.position.x;
                float yDistanceFromTarget = _target.GetTransform().position.y - transform.position.y;
                _atkDirection = new Vector2(xDistanceFromTarget, yDistanceFromTarget).normalized * _meleeAtkLerpDistance;

                //Create our TowardsTarget lerp line
                _toTargetStartPoint = transform.position;
                _toTargetEndPoint = new Vector2(_toTargetStartPoint.x + _atkDirection.x, _toTargetStartPoint.y + _atkDirection.y);

                //Create our BackToOrigin lerp Line (same line, but backwards)
                _toOriginStartPoint = _toTargetEndPoint;
                _toOriginEndPoint = _toTargetStartPoint;

                //Log distance to confirm accuracy
                QuickLogger.ConditionalLog(_isDebugActive, this, $"target's direction from self: {_atkDirection}");

                //set the attack coordinator
                _attackCoordinator = PerformAttackSequence();

                //Start the coordinator
                StartCoroutine(_attackCoordinator);
            }
        }
    }

    private void CancelAttackSequence()
    {
        if (_attackCoordinator != null)
        {
            //interrupt the attack coordinator
            StopCoroutine(_attackCoordinator);

            //exit attack animation
            _animator.SetBool(_atkParam, false);

            //clear attack coordinator
            _attackCoordinator = null;

            //reset TowardsTarget lerping if it's currently active
            if (_isLerpingTowardsTarget)
            {
                _isLerpingTowardsTarget = false;
                _currentLerpToTargetTime = 0;

                //We will automatically lerp back towards the origin if we're offset. Nothing else to reset
            }

            //cooldown Atk
            CooldownAtk();

            //Log status
            QuickLogger.ConditionalLog(_isDebugActive, this, "Cancelled Attack Sequence");
        }
    }

    private void EnterHurtStun()
    {
        //Enter the hurtStun state if not already in it
        if (!_isHurtStunned)
        {
            _isHurtStunned = true;

            //enter animation
            _animator.SetTrigger(_hurtParam);
        }
            

        //reset the remaining hurtStun time to max
        _remainingHurtStunTime = _hurtStunDuration;
    }

    private IEnumerator PerformAttackSequence()
    {
        //Log status
        QuickLogger.ConditionalLog(_isDebugActive, this, "Entered Attack Sequence");

        //Start animation
        _animator.SetBool(_atkParam,true);

        //Begin Lerping our sprite into the atk direction
        _isLerpingTowardsTarget = true;
        _isPositionOffset = true; //make sure we're aware of our offset position in case we cancel the atk early


        //Wait for attack Buildup animation duration
        QuickLogger.ConditionalLog(_isDebugActive, this, "Waiting for attack windup animation time to expire");
        yield return new WaitForSeconds(_attackWindupDuration);



        //Log Status
        QuickLogger.ConditionalLog(_isDebugActive, this, "SWING!");

        //AutoMiss if target doesn't exist anymore
        if (_target == null)
        {
            //play miss sound
            _meleeMiss.Play();

            //Create a miss indicator over this actor
            ShowIndicator(IndicatorType.Miss, "");
        }

        //Else determine hit
        else
        {
            if (DoesAttackHit())
            {
                //communicate damage
                _target.HurtActor(0, DamageType.Physical, _atkOrigin);

                //play hit sound
                _target.PlaySound("meleeHit");

                //create a damage indicator over the hurt actor
                _target.ShowIndicator(IndicatorType.Damage, 0.ToString());
            }

            else
            {
                //play miss sound
                _meleeMiss.Play();

                //Create a miss indicator over this actor
                _target.ShowIndicator(IndicatorType.Miss, "");
            }
            
        }

        //Wait for attack swing animation duration
        QuickLogger.ConditionalLog(_isDebugActive, this, "Waiting for attack swing animation time to expire");
        yield return new WaitForSeconds(_attackRecoveryDuration);


        //End Animation
        _animator.SetBool(_atkParam, false);

        //begin cooling attack
        CooldownAtk();

        //Clear attack coordinator
        _attackCoordinator = null;


        //Log status
        QuickLogger.ConditionalLog(_isDebugActive, this, "Completed Attack Sequence");
    }

    private void PerformCombatBehavior()
    {
        if (_isInCombat)
        {
            //If target exists
            if (_target != null)
            {
                
            }

            //Else leave combat
            else
            {
                QuickLogger.ConditionalLog(_isDebugActive, this, "Leaving Combat");
                _isInCombat = false;
            }
        }


    }

    private void TickHurtStun()
    {
        if (_isHurtStunned)
        {
            //decrement the actors stun time
            _remainingHurtStunTime -= Time.deltaTime;

            //leave the hurt state when the time is up
            if (_remainingHurtStunTime <= 0)
            {
                _isHurtStunned = false;

            }
        }
    }

    private void TickAtkLerp()
    {
        //lerp away if we're building an attack
        if (_isLerpingTowardsTarget)
        {
            _currentLerpToTargetTime += Time.deltaTime;

            //reposition sprite according to our current time
            _spriteParentTransform.position = Vector3.Lerp(_toTargetStartPoint, _toTargetEndPoint, _currentLerpToTargetTime / _lerpToTargetDuration);

            //reset our lerp utils on completion
            if (_currentLerpToTargetTime >= _lerpToTargetDuration)
            {
                _currentLerpToTargetTime = 0;
                _isLerpingTowardsTarget = false;
            }
        }

        //lerp back to our atk origin if we're displaced from it.
        else if (_isPositionOffset && !_isLerpingTowardsTarget)
        {
            _currentLerpToOriginTime += Time.deltaTime;

            //reposition sprite according to our current time
            _spriteParentTransform.position = Vector3.Lerp(_toOriginStartPoint, _toOriginEndPoint, _currentLerpToOriginTime / _lerpBackToOriginDuration);

            //reset our lerp utils on completion
            if (_currentLerpToOriginTime >= _lerpBackToOriginDuration)
            {
                _currentLerpToOriginTime = 0;
                _isPositionOffset = false;
            }
        }
    }

    private void CooldownAtk()
    {
        if (!_isAtkCoolingDown)
        {
            _isAtkCoolingDown = true;
            Invoke(nameof(ReadyAtk), _atkCooldownDuration);
        }
    }

    private void ReadyAtk()
    {
        _isAtkReady = true;
        _isAtkCoolingDown = false;
    }

    private bool DoesAttackHit()
    {
        //Generate number from 1 to 100
        int hitRoll = UnityEngine.Random.Range(1, 101);

        //Determine hit result
        bool result = hitRoll <= _hitChance;

        //Log Result
        QuickLogger.ConditionalLog(_isDebugActive, this, $"Hit Number Rolled: {hitRoll}\nHit Result: {result}");

        //return result
        return result;

    }


    //Externals
    public void HurtActor(int damage, DamageType damageType, Vector2 AttackerPosition)
    {
        //Interrupt and end any in-progress attacks
        CancelAttackSequence();

        //Enter the hurtStun state
        EnterHurtStun();

    }

    public void HealActor(int heal)
    {

    }

    public int GetID()
    {
        return GetInstanceID();
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void PlaySound(string soundFx)
    {
        switch (soundFx)
        {
            case "meleeHit":
                _meleeHit.Play();
                break;

            default:
                QuickLogger.Warn(this, $"SoundFx '{soundFx}' not found");
                break;
        }
    }

    public void SetTarget(IActor actor)
    {
        if (actor != null)
        {
            _DEBUG_isTargetSet = true;
            _target = actor;
        }
    }

    public void ShowIndicator(IndicatorType type, string value)
    {
        GameObject newIndicator = null;
        IndicatorBehavior behavior = null;

        switch (type)
        {
            case IndicatorType.Miss:
                //cache the newly-created object
                newIndicator = _indicatorCreater.CreateNewIndicator(_indicatorContainer, type);

                //cache the object's indicator behavior
                behavior = newIndicator.GetComponent<IndicatorBehavior>();

                //Show the indicator
                behavior.ShowIndicator();

                //leave
                break;


            case IndicatorType.Damage:
                //cache the newly-created object
                newIndicator = _indicatorCreater.CreateNewIndicator(_indicatorContainer, type);

                //cache the object's indicator behavior
                behavior = newIndicator.GetComponent<IndicatorBehavior>();

                //display the damage dealt
                behavior.SetText(value);

                //Show the indicator
                behavior.ShowIndicator();

                //leave
                break;


            case IndicatorType.Heal:
                //cache the newly-created object
                newIndicator = _indicatorCreater.CreateNewIndicator(_indicatorContainer, type);

                //cache the object's indicator behavior
                behavior = newIndicator.GetComponent<IndicatorBehavior>();

                //display the heals gained
                behavior.SetText(value);

                //Show the indicator
                behavior.ShowIndicator();

                //leave
                break;


            case IndicatorType.LevelUp:
                //cache the newly-created object
                newIndicator = _indicatorCreater.CreateNewIndicator(_indicatorContainer, type);

                //cache the object's indicator behavior
                behavior = newIndicator.GetComponent<IndicatorBehavior>();

                //Show the indicator
                behavior.ShowIndicator();

                //leave
                break;


            default:
                QuickLogger.Warn(this, $"No indicator of type {type} exists");
                break;
        }
    }



    //Debug
    public int GetScriptID()
    {
        return GetInstanceID();
    }

    public string GetScriptName()
    {
        return name;
    }

    private void ListenForDebugCommands()
    {
        if (_isDebugActive)
        {
            //update informative values
            if (_target != null)
                _DEBUG_isTargetSet = true;
            else _DEBUG_isTargetSet = false;


            //Listen for commands
            if (_DEBUG_enterAttack_cmd)
            {
                _DEBUG_enterAttack_cmd = false;
                StartNewAttackSequence();
            }

            if (_DEBUG_exitAttack_cmd)
            {
                _DEBUG_exitAttack_cmd = false;
                CancelAttackSequence();
            }

            if (_DEBUG_takeDamage_cmd)
            {
                _DEBUG_takeDamage_cmd = false;
                HurtActor(0, DamageType.Physical, Vector2.zero);
            }

            if (_DEBUG_setTarget_cmd)
            {
                _DEBUG_setTarget_cmd = false;
                _target = _DEBUG_targetActor_param;
                QuickLogger.ConditionalLog(_isDebugActive, this, $"Target Set to {_DEBUG_targetActor_param}");
            }

            if (_DEBUG_spamAttacks_cmd)
            {
                StartNewAttackSequence();
            }
        }
    }

}
