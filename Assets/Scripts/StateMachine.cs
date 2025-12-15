using UnityEngine;

public class StateMachine
{
    public IState CurrentState { get; private set; }

    // Event for debugging
    public event System.Action<IState> OnStateChanged;

    public void Initialize(IState startingState, AIBrain brain)
    {
        CurrentState = startingState;
        CurrentState.Enter(brain);
        OnStateChanged?.Invoke(CurrentState);
    }

    public void ChangeState(IState newState, AIBrain brain)
    {
        if (CurrentState != null)
        {
            CurrentState.Exit(brain);
        }

        CurrentState = newState;
        CurrentState.Enter(brain);
        OnStateChanged?.Invoke(CurrentState);
    }

    public void Update(AIBrain brain)
    {
        if (CurrentState != null)
        {
            CurrentState.Execute(brain);
        }
    }
}