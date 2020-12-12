using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller2D : RaycastController
{
    public float maxSlopeAngle = 80;

    public CollisionInfo collisions;
    [HideInInspector]
    public Vector2 playerInput;

    public override void Start()
    {
        base.Start();

        collisions.faceDir = 1;
    }

    public void Move( Vector2 deltaMove, bool standingOnPlataform)
    {
        Move(deltaMove, Vector2.zero, standingOnPlataform);
    }

    public void Move( Vector2 deltaMove, Vector2 input, bool standingOnPlataform = false)
    {
        UpdateRaycastOrigins();
        collisions.Reset ();

        collisions.velocityOld = deltaMove;
        playerInput = input;
        collisions.playerInput = playerInput;

        if (deltaMove.y < 0)
        {
            DescendSlope(ref deltaMove);
        }

        if (deltaMove.x != 0)
        {
            collisions.faceDir = (int)Mathf.Sign(deltaMove.x);
        }

        HorizontalCollisions(ref deltaMove);

        if (deltaMove.y != 0)
            VerticalCollisions(ref deltaMove);

        transform.Translate(deltaMove);

        if (standingOnPlataform)
        {
            collisions.below = true;
        }
    }

    void HorizontalCollisions(ref Vector2 deltaMove)
    {
        float directionX = collisions.faceDir;
        float rayLenght = Mathf.Abs(deltaMove.x) + skinWidth;

        //si la velocidad es menor que la piel los rayos seran de 2 veces la piel
        if (Mathf.Abs(deltaMove.x) < skinWidth)
        {
            rayLenght = 2 * skinWidth;
        }

        //bucle que itera por todos los rayos que se lanzan en horizontal
        for (int i = 0; i < horizontalRayCount; i++)
        {
            //colocal el origen de los rayos dependiendo de la direcion que se mire y del rayo que sea la altura
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLenght, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLenght, Color.red);

            //si el raycast da con algo
            if (hit)
            {
                //si la distancia es 0 significa que lo estamos atravesando y por tanto no se aplican fisicas horizontales 
                if (hit.distance == 0)
                {
                    continue;
                }

                //el angulo se calcula a traves de la normal de la colision golpeada
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                //si el primer rayo golpea un angulo menor que el maximo angulo que le hemos puesto hara lo siguiente:
                if (i == 0 && slopeAngle <= maxSlopeAngle)
                {
                    //si esta descendiendo ya por una rampa parara de descender por ella y se devolvera la cantidad de movimiento antigua
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        deltaMove = collisions.velocityOld;
                    }

                    float distanceToSlopeStart = 0;
                    //si el angulo que detectamos no es el mismo angulo en el que estamos
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        //calculamos la distancia hasta el siguiente angulo y modificamos la distancia que iba a moverse para que no atraviese el angulo y quede justo encima
                        distanceToSlopeStart = hit.distance - skinWidth;
                        deltaMove.x -= distanceToSlopeStart * directionX;
                    }

                    //ejecutamos la funcion que realiza el escalado de la cuesta pasandole el movimiento el angulo y la normal de la colision
                    ClimbSlope(ref deltaMove, slopeAngle, hit.normal);
                    //la cantidad que se va a mover en x se le devuelve lo que se le habia quitado en caso de ser asi
                    deltaMove.x += distanceToSlopeStart * directionX;
                }

                //si no se esta subiendo ningun angulo o el angulo detectado es mayor que el maximo
                if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle)
                {
                    if (slopeAngle == 90)
                    {
                        collisions.climbingWall = true;
                    }

                    //el movimiento en x sera en caso de que estes frente a una pared 0 y la distancia del rayo sera de la distancia que pueda quedar hasta esta
                    deltaMove.x = Mathf.Min(Mathf.Abs(deltaMove.x), (hit.distance - skinWidth)) * directionX;
                    rayLenght = Mathf.Min(Mathf.Abs(deltaMove.x) + skinWidth, hit.distance);

                    //si estas subiendo una cuesta el movimiento en y pasara a ser la tangente del angulo que estas subiendo por la distancia en x
                    if (collisions.climbingSlope)
                    {
                        deltaMove.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(deltaMove.x);
                    }

                    //se actualiza la direccion
                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }

                if (deltaMove.y <= 0 && i != horizontalRayCount - 1 && collisions.climbingWall)
                    ClimbWall();
            }

            

        }
    }

    void VerticalCollisions(ref Vector2 deltaMove)
    {
        float directionY = Mathf.Sign(deltaMove.y);
        float rayLenght = Mathf.Abs(deltaMove.y) + skinWidth;

        //itera por todos los rayos lanzados en vertical
        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + deltaMove.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLenght, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLenght, Color.red);

            //si el rayo golpea
            if (hit)
            {
                //si la tag es atravesar y estas subiendo lo estas atravesando ya o el input del jugador es hacia abajo se salta las fisicas de este collider
                if (hit.collider.tag == "atravesar")
                {
                    if (directionY == 1 || hit.distance == 0)
                    {
                        continue;
                    }
                    if (playerInput.y == -1)
                    {
                        collisions.fallingThroughPlataform = hit.collider;

                        continue;
                    }
                }

                //si el collider es el que ya estas atravesando sigue si no quitalo para futuras colisiones
                if (hit.collider == collisions.fallingThroughPlataform)
                {
                    continue;
                }
                else
                    collisions.fallingThroughPlataform = null;

                //el movimiento en y sera la distancia hasta la colision y la distancia del rayo lo mismo
                deltaMove.y = (hit.distance - skinWidth) * directionY;
                rayLenght = hit.distance;

                //si se esta subiendo un angulo el movimiento en x sera:
                if (collisions.climbingSlope)
                {
                    deltaMove.x = deltaMove.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(deltaMove.x);
                }

                //si la direccion en y es hacia abajo se pone below en true y above en false y viceversa
                collisions.below = directionY == -1;
                collisions.above = directionY == 1;
            }
        }

        //si se esta subiendo un angulo
        if (collisions.climbingSlope)
        {
            //se crea un rayo que va de los pies de nuesta caja hacia el lado que nos movemos
            float directionX = Mathf.Sign(deltaMove.x);
            rayLenght = Mathf.Abs(deltaMove.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * deltaMove.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLenght, collisionMask);

            //si el rayo golpea
            if (hit)
            {
                //calcula el nuevo angulo
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                //si el nuevo angulo no es el angulo en el que estamos 
                if (slopeAngle != collisions.slopeAngle)
                {
                    deltaMove.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                    collisions.slopeNormal = hit.normal;
                }
            }
        }
    }

    void ClimbSlope (ref Vector2 deltaMove, float slopeAngle, Vector2 slopeNormal)
    {
        //la distancia es la distancia en x y lo que hay que subir en y se obtine mediante la ecucion siguiente
        float moveDistance = Mathf.Abs(deltaMove.x);
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        //si lo que se mueve en y es menor o igual que lo que deberia subir se hace:
        if (deltaMove.y <= climbVelocityY)
        {
            //el movimiento en y sera el calculado y el movimiento en x seguira la siguiente ecuacion
            deltaMove.y = climbVelocityY;
            deltaMove.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(deltaMove.x);

            //las colisiones se actualizan para hacer saber al programa que aunque estes subiendo estas en el suelo y se guardan los datos del angulo
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
            collisions.slopeNormal = slopeNormal;
        }
        
    }

    void DescendSlope (ref Vector2 deltaMove)
    {
        //se crean dos raycast uno para cada esquina inferior de la caja
        RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(deltaMove.y) + skinWidth, collisionMask);
        RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(deltaMove.y) + skinWidth, collisionMask);

        //si uno de los dos golpea y el otro no se calcula si debe deslizarse
        if (maxSlopeHitRight ^ maxSlopeHitLeft)
        {
            SlideDownMaxSlope(maxSlopeHitLeft, ref deltaMove);
            SlideDownMaxSlope(maxSlopeHitRight, ref deltaMove);
        }

        //si no se esta deslizando
        if (!collisions.slidingDownMaxSlope)
        {
            //lanza un rayo desde la direccion que te estas moviendo hacia abajo para detectar el siguiente angulo
            float directionX = Mathf.Sign(deltaMove.x);
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, Mathf.Infinity, collisionMask);

            //si el rayo golpea:
            if (hit)
            {
                //el nuevo angulo es el golpeado
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                //si el angulo no es cero y es menor o igual que el maximo
                if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle)
                {
                    if (Mathf.Sign(hit.normal.x) == directionX)
                    {
                        //si la distancia hacia el angulo es menor o igual que la tangente de este 
                        if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(deltaMove.x))
                        {
                            //lo bajas sin problemas
                            float moveDistance = Mathf.Abs(deltaMove.x);
                            float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                            deltaMove.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(deltaMove.x);
                            deltaMove.y -= descendVelocityY;

                            collisions.slopeAngle = slopeAngle;
                            collisions.descendingSlope = true;
                            collisions.below = true;
                            collisions.slopeNormal = hit.normal;
                        }
                    }
                }
            }
        }
    }

    void SlideDownMaxSlope (RaycastHit2D hit, ref Vector2 deltaMove)
    {
        //si ha golpeado el raycast que le hemos pasado
        if (hit)
        {
            //el nuevo angulo a tratar sera el siguiente
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            //si el angulo es mayor que el maximo angulo posible 
            if (slopeAngle > maxSlopeAngle)
            {
                //se resbala
                deltaMove.x = Mathf.Sign(hit.normal.x) * (Mathf.Abs(deltaMove.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

                collisions.slopeAngle = slopeAngle;
                collisions.slidingDownMaxSlope = true;
                collisions.slopeNormal = hit.normal;
            }
        }
    }

    void ClimbWall()
    {
        Vector2 rayOrigin = ((collisions.faceDir == -1) ? raycastOrigins.topLeft : raycastOrigins.topRight);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * collisions.faceDir, 5*skinWidth, collisionMask);

        Debug.DrawRay(rayOrigin, Vector2.right * collisions.faceDir * 5 * skinWidth, Color.yellow);

        if (hit && hit.collider.tag == "agarrar")
            collisions.seAgarra = true;

    }

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public bool climbingSlope, descendingSlope, climbingWall;
        public float slopeAngle, slopeAngleOld;
        public bool slidingDownMaxSlope;
        public Vector2 slopeNormal;

        public bool seAgarra;

        public Vector2 velocityOld;

        public int faceDir;
        public Vector2 playerInput;

        public Collider2D fallingThroughPlataform;

        public void Reset ()
        {
            above = below = false;
            left = right = false;
            climbingSlope = descendingSlope = slidingDownMaxSlope = climbingWall = seAgarra = false;
            slopeNormal = Vector2.zero;

            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }
}
