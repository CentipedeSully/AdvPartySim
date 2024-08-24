using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public enum DamageType
{
    Physical,
    Magical
}


public interface IActor
{
    void HurtActor(int damage, DamageType damageType, Vector2 AttackerPosition);

    void HealActor(int heal);

    int GetID();

    Transform GetTransform();

}

public class ActorBehavior : MonoBehaviour, IActor, IQuickLoggable
{
    //Declarations
    [Header("Combat")]
    private bool _isInCombat = false;
    private bool _isAtkReady = true;
    private bool _isAtkCoolingDown = false;
    [SerializeField] [Min(.5f)] private float _atkCooldown = .5f;
    [SerializeField] private IActor _target;
    [SerializeField] private Transform _spriteParentTransform;

    [SerializeField] private float _meleeAtkDisplacementOffset = .3f;
    private Vector2 _atkOrigin;
    private Vector2 _atkDirection = Vector2.zero;
    private bool _isPositionOffset = false;

    private bool _isLerpingTowardsTarget = false;
    private float _currentLerpToTargetTime = 0;
    [Tooltip("This should at least match the atk's windup time")]
    [SerializeField] private float _lerpToTargetDuration;
    private Vector2 _toTargetStartPoint;
    private Vector2 _toTargetEndPoint;

    private float _currentLerpToOriginTime = 0;
    [Tooltip("This should at most last however long it takes the atk to recover")]
    [SerializeField] private float _lerpBackToOriginDuration;
    private Vector2 _toOriginStartPoint;
    private Vector2 _toOriginEndPoint;

    [SerializeField] private float _attackWindupDuration;
    [SerializeField] private float _attackSwingDuration;
    [SerializeField] private float _hurtStunDuration;
    private bool _isHurtStunned = false;
    private float _remainingHurtStunTime = 0;

    private IEnumerator _attackCoordinator;

    [Header("Animation")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _atkParam = "isAttacking";
    [SerializeField] private string _hurtParam = "isHurt";

    [Header("Debug")]
    [SerializeField] private bool _isDebugActive = false;
    [SerializeField] private bool _DEBUG_setTarget_cmd = false;
    [SerializeField] private ActorBehavior _DEBUG_targetActor_param;
    [SerializeField] private bool _DEBUG_spamAttacks_cmd = false;
    [SerializeField] private bool _DEBUG_enterAttack_cmd = false;
    [SerializeField] private bool _DEBUG_exitAttack_cmd = false;
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
                _atkDirection = new Vector2(xDistanceFromTarget, yDistanceFromTarget).normalized * _meleeAtkDisplacementOffset;

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

        //CastAttack
        if (_target == null)
        {
            //play miss sound
        }

        //Wait for attack swing animation duration
        QuickLogger.ConditionalLog(_isDebugActive, this, "Waiting for attack swing animation time to expire");
        yield return new WaitForSeconds(_attackSwingDuration);


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
            Invoke(nameof(ReadyAtk), _atkCooldown);
        }
    }

    private void ReadyAtk()
    {
        _isAtkReady = true;
        _isAtkCoolingDown = false;
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
