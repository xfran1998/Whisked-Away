using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Player))]
public class PlayerInput : MonoBehaviour
{
    Player player;
    Controller2D controller;

    void Start()
    {
        player = GetComponent<Player>();
        controller = GetComponent<Controller2D>();
    }

    void Update()
    {
        Vector2 directionalInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        player.SetDireccionalInput(directionalInput);


        if (Input.GetKeyDown(KeyCode.Space))
        {
            player.OnJumpInputDown();
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            player.OnJumpInputUp();
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            player.ToggleRun();
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            player.PrepareJump();
        }

        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            player.UnprepareJump();
        }


    }
}
