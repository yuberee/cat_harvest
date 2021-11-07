﻿using Sandbox;
using System;
using System.Linq;

namespace Cat_Harvest
{
	public partial class WalkingCat : AnimEntity
	{

		private readonly Vector3 minBounds = new Vector3( -800, -770, 0 );
		private readonly Vector3 maxBounds = new Vector3( 750, 790, 0 );

		public override void Spawn()
		{

			base.Spawn();

			Tags.Add( "Cat" );

			SetModel( "models/cat/cat.vmdl" );
			Scale = 1f;
			CollisionGroup = CollisionGroup.Prop;
			SetupPhysicsFromCapsule( PhysicsMotionType.Keyframed, Capsule.FromHeightAndRadius( 16, 2 ) ); //Remove collisions when done? Can't pick them up in final game anyways

		}

		float nextMove = 0f;

		[Event.Tick.Server]
		public void Tick()
		{
			
			//SetAnimFloat( "move_x", 35f * Velocity.Length );

			float friction = 0.2f;

			if ( nextMove <= Time.Now )
			{

				if ( Position.x <= minBounds.x || Position.x >= maxBounds.x || Position.y <= minBounds.y || Position.y >= maxBounds.y )
				{

					Velocity = ( Vector3.Zero - Position ).ClampLength( 10f );

				}
				else
				{

					Velocity = new Vector3( Rand.Float( 10f ) - 5f, Rand.Float( 10f ) - 5f, 0f );

				}

				nextMove = Time.Now + 6f + Rand.Float( 2f );

			}

			TraceResult traceGround = Trace.Ray( Position + Vector3.Up * 16, Position + Vector3.Down * 32 )
				.Ignore( this )
				.WithoutTags( "Player" )
				.WithoutTags( "Cat" )
				.Run();

			if ( traceGround.Hit )
			{

				Position = traceGround.EndPos;
				Position += Rotation.Forward * 10 * Velocity.Length * Time.Delta;

				Rotation rotation = Velocity.EulerAngles.ToRotation();
				Rotation = Rotation.Slerp( Rotation, rotation, 2 * Time.Delta );

			}
			else
			{

				Position += Vector3.Down * 100 * Time.Delta;

			}

			Velocity *= 1 - Time.Delta * friction;

		}

		[ServerCmd("spawncat")] //TODO REMEMBER DELETE!!!
		public static void SpawnCat()
		{

			var pos = ConsoleSystem.Caller.Pawn.Position;

			for ( int i = 0; i < 96; i++ )
			{

				var npc = new WalkingCat
				{

					Position = pos

				};

			}

		}

	}

}
