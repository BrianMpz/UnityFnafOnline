using System.Collections;
using UnityEngine;

public class JanitorPlayerBehaviour : PlayerBehaviour
{
    public override bool IsPlayerVulnerable(Node currentNode)
    {
        throw new System.NotImplementedException();
    }

    public override IEnumerator WaitUntilKillConditionsAreMet(Node currentNode)
    {
        throw new System.NotImplementedException();
    }

    private protected override IEnumerator PlayDeathAnimation(string deathScream)
    {
        throw new System.NotImplementedException();
    }

    private protected override void UpdateCameraView()
    {
        throw new System.NotImplementedException();
    }

    private protected override void UpdatePowerUsage()
    {
        throw new System.NotImplementedException();
    }

    public override void PowerOff()
    {
        // this player cant power off so dont implement
    }

    void Start()
    {

    }

}
