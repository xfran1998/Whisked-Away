using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Controller2D))]
public class Player : MonoBehaviour
{
    public float maxJumpHeight = 2;
    public float minJumpHeight = 1;
    public float timeToJumpApex = .4f;
    float accelerationTimeAirborn = .2f;
    float accelerationTimeGrounded = .1f;
    float moveSpeed = 5;
    float lastDirection;

    public Vector2 wallJumpClimb = new Vector2 (7, 18);
    public Vector2 wallJumpOff = new Vector2 (8.5f, 7);
    public Vector2 wallLeap = new Vector2 (18, 17);

    public float wallSlideSpeedMax = 3;
    public float wallStickTime = .1f;
    [HideInInspector]
    public float timeToWallUnstick;

    float maxJumpVelocity;
    float minJumpVelocity;
    float gravity;
    [HideInInspector]
    public Vector3 velocity;

    float velocityXSmoothing;

    [HideInInspector]
    public bool IsRunning = false;
    [HideInInspector]
    public bool Prep = false;

    Controller2D controller;

    [HideInInspector]
    public Vector2 directionalInput;
    [HideInInspector]
    public bool wallSliding;
    [HideInInspector]
    public int wallDirX;

    [HideInInspector]
    public bool wallJumpUp, wallJumpOut, wallJumpLeap;

    [HideInInspector]
    public bool sube = false;
    [HideInInspector]
    float altura = 5, alturaMax = 5;

    void Start()
    {
        controller = GetComponent<Controller2D> ();

        //se inicializa la gravedad como menos dos veces la altura maxima del salto dividido por el tiempo para llegar ahi al cuadrado
        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        //la velocidad para el maximo salto sera la gravedad multiplicado por el tiempo a llegar a la altura maxima
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        //la velocidad para alcanzar el salto minimo sera la raiz cuadrada de dos veces la gravedad por la altura minima
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
        print("Gravity: " + gravity + " Jump Velocity: " + maxJumpVelocity);
    }

    void Update()
    {
        
        //se recalcula la velocidad y las paredes cada frame
        CalculateVelocity();
        HandleWallSliding();
        ClimbWall();

        //se hace el movimiento
        controller.Move(velocity * Time.deltaTime, directionalInput);

        //si hay colisiones arriba o abajo
        if (controller.collisions.above || controller.collisions.below)
        {
            //si estas deslizandote la velocidad en y se recalcula si no la velocidad es 0
            if (controller.collisions.slidingDownMaxSlope)
            {
                velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
            }
            else
            {
                velocity.y = 0;
            }
        }
    }

    public void SetDireccionalInput (Vector2 input)
    {
        directionalInput = input;

        if (directionalInput.x != 0)
            lastDirection = directionalInput.x;
    }

    public void OnJumpInputDown ()
    {
        //si estas deslizandote por una pared
        if (wallSliding)
        {
            //si estas apuntando hacia la pared, si no estas manteniendo nada o si apuntas al contrario haras saltos diferentes
            if (wallDirX == directionalInput.x)
            {
                velocity.x = -wallDirX * wallJumpClimb.x;
                velocity.y = wallJumpClimb.y;
                wallJumpUp = true;
            }
            else if (directionalInput.x == 0)
            {
                velocity.x = -wallDirX * wallJumpOff.x;
                velocity.y = wallJumpOff.y;
                wallJumpOut = true;
            }
            else
            {
                velocity.x = -wallDirX * wallLeap.x;
                velocity.y = wallLeap.y;
                wallJumpLeap = true;
            }
        }

        if (controller.collisions.below)
            if (controller.collisions.slidingDownMaxSlope)
            {
                // para no poder saltar contra paredes que superan el angulo -- seguro que se puede hacer de otra manera
                if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x))
                {
                    velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y * 2;
                    velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x * 2;
                }
            }
            else
            {
                if (Prep)
                {
                    velocity.y = maxJumpVelocity / 2;
                    velocity.x = maxJumpVelocity * 4 * lastDirection;
                }
                else
                    velocity.y = maxJumpVelocity;
            }
    }

    public void OnJumpInputUp ()
    {
        if (velocity.y > minJumpVelocity)
        {
            velocity.y = minJumpVelocity;
        }
    }

    public void ToggleRun()
    {
        if (IsRunning)
        {
            moveSpeed = moveSpeed / 2;
            IsRunning = false;
        }
        else
        {
            moveSpeed = moveSpeed * 2;
            IsRunning = true;
        }
    }

    public void PrepareJump()
    {
        if (controller.collisions.below)
        {
            moveSpeed = 0;
            Prep = true;
        }
    }

    public void UnprepareJump()
    {
        if (directionalInput.x != 0)
        {
            moveSpeed = 10;
            IsRunning = true;
        }
        else
        {
            moveSpeed = 5;
            IsRunning = false;
        }

        Prep = false;
    }

    void ClimbWall()
    {
        //solo puede empezar a escalar en el suelo pegado a una pared de 90º y moviendote hacia ella mientras corres
        if ((controller.collisions.below && IsRunning || sube) && wallDirX == directionalInput.x && controller.collisions.climbingWall)
        {
            sube = true;
            timeToWallUnstick = wallStickTime;

            altura -= velocity.y * Time.deltaTime;

            if (altura > 0)
            {
                velocity.y = moveSpeed;
            }
            else
            {
                sube = false;
            }

            velocityXSmoothing = 0;
            velocity.x = 0;
        }
        else
        {
            sube = false;
            altura = alturaMax;
        }
    }

    void HandleWallSliding ()
    {
        //coloca la direccion de la pared
        wallDirX = (controller.collisions.left) ? -1 : 1;

        //reseteamos
        wallSliding = false;
        //si esta cayendo en una pared
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            wallSliding = true;

            //si cae que no sea tan rapido
            if (velocity.y < -wallSlideSpeedMax)
            {
                velocity.y = -wallSlideSpeedMax;
            }

            //para soltarse de la pared
            if (timeToWallUnstick > 0)
            {
                velocityXSmoothing = 0;
                velocity.x = 0;

                if (directionalInput.x != wallDirX && directionalInput.x != 0)
                    timeToWallUnstick -= Time.deltaTime;
                else
                {
                    timeToWallUnstick = wallStickTime;
                }
            }
            else
            {
                timeToWallUnstick = wallStickTime;
            }

        }
        //manejar la velocidad de escalado aqui??
    }

    void CalculateVelocity ()
    {
        //el sitio al que moverse sera:
        float targetVelocityX = directionalInput.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborn);
        velocity.y += gravity * Time.deltaTime;
    }
}