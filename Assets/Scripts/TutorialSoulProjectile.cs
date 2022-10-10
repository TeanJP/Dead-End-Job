using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialSoulProjectile : SoulProjectile
{
    private TutorialController tutorialController = null;

    protected override void Start()
    {
        base.Start();
        tutorialController = gameController.tutorialController;
    }

    protected override void Update()
    {
        MoveProjectile();

        if (destroyProjectile)
        {
            tutorialController.CompleteGoal(3, 1f);
            Destroy(gameObject);
        }
    }
}
