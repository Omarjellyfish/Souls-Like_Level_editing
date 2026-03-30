using System;
using UnityEngine;

public enum SoulsLikePlayerState
{
    Idle,
    Moving,
    Sprinting,
    Airborne,
    WallRunning,
    Attacking,
    Dodging,
    Parrying,
    Staggered,
    Dead
}

public class SoulsLike_StateManager : MonoBehaviour
{
    [Header("Current State (Read Only)")]
    [SerializeField] private SoulsLikePlayerState _currentState = SoulsLikePlayerState.Idle;
    
    public SoulsLikePlayerState CurrentState => _currentState;

    public event Action<SoulsLikePlayerState> OnStateChanged;

    /// <summary>
    /// Ask permission to enter a state. Returns true if allowed and transitions. Returns false if blocked.
    /// Example: You cannot enter Attacking if you are already Dodging.
    /// </summary>
    public bool TryEnterState(SoulsLikePlayerState newState)
    {
        if (_currentState == SoulsLikePlayerState.Dead) return false;

        // Death overrides everything immediately
        if (newState == SoulsLikePlayerState.Dead)
        {
            ForceState(newState);
            return true;
        }

        // Staggered overrides almost everything except death
        if (newState == SoulsLikePlayerState.Staggered)
        {
            ForceState(newState);
            return true;
        }

        // If we are currently staggered, we cannot do anything until we decide to exit manually (usually via Animation Event)
        if (_currentState == SoulsLikePlayerState.Staggered && newState != SoulsLikePlayerState.Idle && newState != SoulsLikePlayerState.Dead) 
            return false;

        // Specific Transition Rules to prevent overlapping combat systems
        switch (newState)
        {
            case SoulsLikePlayerState.Attacking:
            case SoulsLikePlayerState.Parrying:
                // Only allowed if we are not already doing an action
                if (_currentState == SoulsLikePlayerState.Dodging || 
                    _currentState == SoulsLikePlayerState.Attacking || 
                    _currentState == SoulsLikePlayerState.Parrying ||
                    _currentState == SoulsLikePlayerState.WallRunning)
                    return false;
                break;

            case SoulsLikePlayerState.Dodging:
                // Cannot dodge if we are already dodging or parrying or in the air
                if (_currentState == SoulsLikePlayerState.Dodging || 
                    _currentState == SoulsLikePlayerState.Parrying ||
                    _currentState == SoulsLikePlayerState.Airborne ||
                    _currentState == SoulsLikePlayerState.WallRunning)
                    return false;
                // Note: We DO allow dodging while Attacking to act as an animation cancel if needed!
                break;

            case SoulsLikePlayerState.WallRunning:
                // Must be jumping or sprinting to start wall run
                if (_currentState != SoulsLikePlayerState.Airborne && _currentState != SoulsLikePlayerState.Sprinting)
                    return false;
                break;
                
            case SoulsLikePlayerState.Airborne:
                 // Jumping is totally blocked if swinging a sword or dodging
                 if (_currentState == SoulsLikePlayerState.Attacking || 
                     _currentState == SoulsLikePlayerState.Dodging || 
                     _currentState == SoulsLikePlayerState.Parrying)
                     return false;
                 break;
        }

        // Rule passed! Make the transition.
        ForceState(newState);
        return true;
    }

    /// <summary>
    /// Instantly forces a state without checking transition rules via TryEnterState.
    /// Used largely for exiting back to Idle when animations finish.
    /// </summary>
    public void ForceState(SoulsLikePlayerState newState)
    {
        if (_currentState == newState) return;
        
        _currentState = newState;
        OnStateChanged?.Invoke(_currentState);
    }
}
