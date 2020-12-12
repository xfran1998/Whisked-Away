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
    float lastDirection = 1;

    public Vector2 wallJumpClimb = new Vector2 (2, 20);
    //public Vector2 wallJumpOff = new Vector2 (8.5f, 7);
    public Vector2 wallLeap = new Vector2 (20, 2);

    public float wallSlideSpeedMax = 3;
    public float wallStickTime = .2f;
    [HideInInspector]
    public float timeToWallUnstick;

    float maxJumpVelocity;
    float minJumpVelocity;
    float gravity;
    [HideInInspector]
    public Vector3 velocity;

    float velocityXSmoothing;

    [HideInInspector]
    public bool isRunning = false;
    [HideInInspector]
    public bool prep = false;
    float longJump = 1;
    bool prepare = true, jumped = false;

    GameObject estela, fantasma;
    float control = 10;

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
        controller = GetComponent<Controller2D>();

        //se inicializa la gravedad como menos dos veces la altura maxima del salto dividido por el tiempo para llegar ahi al cuadrado
        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        //la velocidad para el maximo salto sera la gravedad multiplicado por el tiempo a llegar a la altura maxima
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        //la velocidad para alcanzar el salto minimo sera la raiz cuadrada de dos veces la gravedad por la altura minima
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
        print("Gravity: " + gravity + " Jump Velocity: " + maxJumpVelocity);
        
        //coge todo lo de los hijos y los hace invisible
        estela = this.transform.GetChild(0).gameObject;
        estela.GetComponent<SpriteRenderer>().enabled = false;
        fantasma = this.transform.GetChild(1).gameObject;
        fantasma.GetComponent<SpriteRenderer>().enabled = false;
    }

    void Update()
    {
        
        //se recalcula la velocidad y las paredes cada frame
        CalculateVelocity();
        HandleWallSliding();
        ClimbWall();

        if (controller.collisions.seAgarra && velocity.y <= 0)
            velocity.y = 0;

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

        if (prep && directionalInput.x != lastDirection && directionalInput.x != 0)
            longJump = 1;

        if (directionalInput.x != 0)
            lastDirection = directionalInput.x;
    }

    public void OnJumpInputDown ()
    {
        //si estas deslizandote por una pared
        if (wallSliding)
        {
            //si estas apuntando hacia la pared, si no estas manteniendo nada o si apuntas al contrario haras saltos diferentes
            if (wallDirX == directionalInput.x && controller.collisions.seAgarra)
            {
                velocity.x = -wallDirX * wallJumpClimb.x;
                velocity.y = wallJumpClimb.y;
                wallJumpUp = true;
            }
            else if (wallDirX != directionalInput.x && directionalInput.x != 0)
            {
                velocity.x = -wallDirX * wallLeap.x;
                velocity.y = wallLeap.y;
                wallJumpLeap = true;
            }
            /*else
            {
                velocity.x = -wallDirX * wallJumpOff.x;
                velocity.y = wallJumpOff.y;
                wallJumpOut = true;
            }*/
            
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
                if (prep)
                {
                    velocity.y = 2*maxJumpVelocity/3;
                    velocity.x = maxJumpVelocity * longJump * lastDirection;

                    estela.GetComponent<SpriteRenderer>().enabled = false;
                    longJump = 1;
                    jumped = true;
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
        if (isRunning)
        {
            moveSpeed = moveSpeed / 2;
            isRunning = false;
        }
        else
        {
            moveSpeed = moveSpeed * 2;
            isRunning = true;
        }
    }

    public void PrepareJump()
    {
        //salto maximo 8.5
        //salto minimo 2.1

        int maxJump = 4, minJump = 1;

        if (controller.collisions.below && !jumped)
        {
            moveSpeed = 0;
            prep = true;

            //hacer los limites del salto
            if (longJump >= maxJump)
                prepare = false;
            else if (longJump <= minJump)
                prepare = true;

            if (prepare)
                longJump += 2 * Time.deltaTime;
            else
                longJump -= 2 * Time.deltaTime;
            //hacer los limites del salto

            EstelaSalto();
        }
        else
            jumped = false;
    }

    void EstelaSalto()
    {
        //probablemente se puede hacer con un solo hijo reposicionando el otro cada vez que quieras calcular los raycast
        //se puede enviar un solo raycast (el que este en la direccion de movimiento) y si no golpea lanzar el otro para comprobar
        Bounds bounds = fantasma.GetComponent<BoxCollider2D>().bounds;
        float dstY = 0, raydistance = 5;

        // A(1,2.1) B(4,8.5)
        // (x-1)/(4-1) = (y-2.1)/(8.5-2.1)
        // pos = (8.5-2.1)*((longJump-minJump)/(maxJump-minJump) + 2.1/(8.5-2.1))

        //medJump = ((6.4) * (((longJump - minJump) / (maxJump - minJump)) + (2.1 / (6.4))));
        float medJump = (float)((((longJump - 1) / 3) + (2.1 / 6.4)) * 6.4);

        fantasma.transform.position = this.transform.position + new Vector3(medJump * lastDirection, (float)(0.5), 0);

        //calcular la y con raycast
        RaycastHit2D hitIzq = Physics2D.Raycast(new Vector2(bounds.min.x, bounds.min.y), Vector2.down, raydistance);
        RaycastHit2D hitDer = Physics2D.Raycast(new Vector2(bounds.max.x, bounds.min.y), Vector2.down, raydistance);

        Debug.DrawRay(new Vector2(bounds.min.x, bounds.min.y), Vector2.down * raydistance, Color.yellow);
        Debug.DrawRay(new Vector2(bounds.max.x, bounds.min.y), Vector2.down * raydistance, Color.yellow);

        estela.GetComponent<SpriteRenderer>().enabled = true;

        if (hitDer && hitIzq)
        {
            dstY = (float)(hitDer.distance - Mathf.Abs(fantasma.transform.position.y - this.transform.position.y) + bounds.size.y);
        }
        else
        {
            estela.GetComponent<SpriteRenderer>().enabled = false;
        }

        //podria hacer que cuando se acerque a un borde se haga mas pequeño hasta desaparecer
        //calcular la y con raycast

        if (hitDer.distance == 0 && hitIzq.distance == 0)
        {
            estela.GetComponent<SpriteRenderer>().enabled = false;
        }

        //si golpea una pared no avances mas

        if (hitDer && hitIzq && (hitDer.distance == 0 || hitIzq.distance == 0))
        {
            prepare = false;
        }
        else
            estela.transform.position = this.transform.position + new Vector3(medJump * lastDirection, -dstY, 0);

        //si golpea una pared no avances mas


    }

    public void UnprepareJump()
    {
        if (directionalInput.x != 0)
        {
            moveSpeed = 10;
            isRunning = true;
        }
        else
        {
            moveSpeed = 5;
            isRunning = false;
        }

        longJump = 1;
        estela.GetComponent<SpriteRenderer>().enabled = false;
        prep = false;
    }

    void ClimbWall()
    {
        //solo puede empezar a escalar en el suelo pegado a una pared de 90º y moviendote hacia ella mientras corres
        if ((controller.collisions.below && isRunning || sube) && wallDirX == directionalInput.x && controller.collisions.climbingWall)
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
                    directionalInput.x = wallDirX;
                }
            }
            else
            {
                timeToWallUnstick = wallStickTime;
            }

        }
    }

    void CalculateVelocity ()
    {
        //el sitio al que moverse sera:
        float targetVelocityX = directionalInput.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborn);
        velocity.y += gravity * Time.deltaTime;
    }
}