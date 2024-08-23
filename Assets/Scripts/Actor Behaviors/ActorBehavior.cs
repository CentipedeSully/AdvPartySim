using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private IActor _target;
    [SerializeField] private float _meleeAtkDisplacementOffset = .3f;
    private Vector2 _atkDirection = Vector2.zero;
    [SerializeField] private float _attackWindupDuration;
    [SerializeField] private float _attackSwingDuration;
    [SerializeField] private float _hurtStunDuration;

    [SerializeField] private bool _isHurtStunned = false;
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
    [SerializeField] private bool _DEBUG_enterAttack_cmd = false;
    [SerializeField] private bool _DEBUG_exitAttack_cmd = false;
    [SerializeField] private bool _DEBUG_takeDamage_cmd = false;

    //Monobehaviors

    private void Update()
    {
        ListenForDebugCommands();
        TickHurtStun();
    }


    //Internals
    private void StartNewAttackSequence()
    {
        if (_attackCoordinator != null)
        {
            QuickLogger.Warn(this, "Attempted to start a new attack when one was already in progress. Ignoring Request");
            return;
        }

        //Only start an attack if we have a target
        else if (_target != null)
        {
            //Calculate the direction from ourself to the target
            Vector3 directionTorwardsTarget = _target.GetTransform().position - transform.position;
            QuickLogger.ConditionalLog(_isDebugActive, this, $"target's direction from self: {directionTorwardsTarget}");

            //convert the direction into simpler (X,Y) form -- no Depth exists
            _atkDirection = new Vector2(directionTorwardsTarget.x, directionTorwardsTarget.y);


            //set the attack coordinator
            _attackCoordinator = PerformAttackSequence();

            //Start the coordinator
            StartCoroutine(_attackCoordinator);
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
        }
    }


}
