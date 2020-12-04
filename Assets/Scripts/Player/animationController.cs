using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
public class animationController : MonoBehaviour
{
    Animator animator;
    PlayerInput input;
    Player player;
    Controller2D controller;

    private string currentState;

    void Start()
    {
        animator = GetComponent<Animator>();
        input = GetComponent<PlayerInput>();
        player = GetComponent<Player>();
        controller = GetComponent<Controller2D>();
    }

    void ChangeAnimationState(string newState)
    {
        //hace que la misma animacion no se pare a si misma
        if (currentState == newState) return;

        //empieza una animacion
        animator.Play(newState);

        //reasigna el nuevo estado
        currentState = newState;
    }

    void Update()
    {
        //jumping 1
        if (controller.collisions.below)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (!animator.GetCurrentAnimatorStateInfo(0).IsName("PrepLoop"))
                {
                    ChangeAnimationState("Prep1");
                }
                if (animator.GetCurrentAnimatorStateInfo(0).IsName("Prep1") && !(animator.GetCurrentAnimatorStateInfo(0).length > animator.GetCurrentAnimatorStateInfo(0).normalizedTime))
                {
                    ChangeAnimationState("PrepLoop");
                }
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    ChangeAnimationState("LongJump");
                }
            }
            else
                if (player.directionalInput.x != 0)
                {
                    if (player.IsRunning)
                    {     
                        ChangeAnimationState("Run");
                    }
                    else
                        ChangeAnimationState("Walk");
                }
                else
                    ChangeAnimationState("idle");
        }
        else
        {
            if (!animator.GetCurrentAnimatorStateInfo(0).IsName("LongJump"))
            {
                if (player.velocity.y > 0)
                {
                    if (controller.collisions.climbingWall)
                    {
                        ChangeAnimationState("Climbing");
                    }
                    else
                        ChangeAnimationState("JumpUp");
                }
                else
                {
                    if (player.wallSliding)
                    {
                        if (player.timeToWallUnstick < .2f)
                            ChangeAnimationState("LeapingWall");
                        else
                            ChangeAnimationState("sliding");
                    }
                    else
                        ChangeAnimationState("JumpDown");
                }
            }
        }
        

        //si esta dado la vuelta
        if (player.directionalInput.x == -1)
            GetComponent<SpriteRenderer>().flipX = true;
        if (player.directionalInput.x == 1)
            GetComponent<SpriteRenderer>().flipX = false;

    }  
}
