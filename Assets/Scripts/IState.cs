using UnityEngine;

public interface IState
{
    void Enter(AIBrain brain);
    void Execute(AIBrain brain);
    void Exit(AIBrain brain);
}