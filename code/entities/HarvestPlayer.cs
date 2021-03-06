using Sandbox;
using Sandbox.entities;
using System;
using System.Collections.Generic;

namespace Cat_Harvest
{

	public partial class HarvestPlayer : Player
	{

		[Net] public int CatsUprooted { get; set; } = 0;
		[Net] public int CatsHarvested { get; set; } = 0;
		public bool OpenInventory { get; set; } = false;
		public bool CloseInstructions { get; set; } = false;
		[Net] public bool DisplayPopup { get; set; } = false;
		[Net] public bool DisplaySecretPopup { get; set; } = false;
		[Net] public bool HasCat { get; set; } = false;
		[Net] public Vector3 LookPos { get; set; } = new Vector3( 0, 0, 0 );
		public HarvestViewModel ViewModel { get; set; }

		public override void Respawn()
		{

			SetModel( "models/citizen/citizen.vmdl" );

			Tags.Add( "Player" );

			Controller = new WalkController() { WalkSpeed = 100.0f, DefaultSpeed = 100.0f, SprintSpeed = 160.0f };

			Animator = new StandardPlayerAnimator();

			CameraMode = new FirstPersonCamera();

			CreateViewModel();

			EnableAllCollisions = true;
			EnableDrawing = false;

			base.Respawn();

		}

		TimeSince lastStep = 0f;
		TimeSince autoClose = 0f;

		public override void Simulate( Client cl )
		{

			base.Simulate( cl );

			if ( IsClient )
			{

				if ( Input.Down( InputButton.Score ) )
				{

					OpenInventory = true;
					CloseInstructions = true;

				}
				else
				{

					OpenInventory = false;

				}

				if( autoClose >= 15f )
				{

					CloseInstructions = true;

				}

			}

				/*LookPos = Trace.Ray( Input.Cursor, 150f )
					.Ignore( this )
					.Run().EndPos;*/

				TraceResult eyeTrace = Trace.Ray( Input.Cursor, 100f )
				.Size( new Vector3( 20f, 20f, 20f ) )
				.Ignore( Map.Entity )
				.WithTag( "Cat" )
				.Run();

			if ( eyeTrace.Hit )
			{

				if ( !HasCat )
				{

					HarvestGame current = HarvestGame.Current as HarvestGame;
					var cat = eyeTrace.Entity;

					if ( cat == current.SecretCat )
					{

						TraceResult secretTrace = Trace.Ray( Input.Cursor, 70f )
							.Size( new Vector3( 10f, 10f, 10f ) )
							.Ignore( Map.Entity )
							.WithTag( "Cat" )
							.Run();

						if ( secretTrace.Entity == current.SecretCat )
						{

							DisplaySecretPopup = true;

							if ( Input.Pressed( InputButton.Use ) )
							{

								if ( IsServer )
								{

									cat.Delete();

									SetAnim( "wiwi", true );
									HarvestGame.EndGame( this, 0, true );

								}

							}

						}

					}
					else
					{

						DisplayPopup = true;

						if ( Input.Pressed( InputButton.Use ) )
						{

							Sound.FromWorld( $"meow{ Rand.Int( 10 ) }", cat.Position );
							Particles.Create( "particles/uproot.vpcf", cat.Position );

							if ( IsServer )
							{

								cat.Delete();

								GameServices.SubmitScore( Client.PlayerId, 1 );
								CatsUprooted++;
								SetAnim( "grab", true );
								HasCat = true;

							}

						}

					}

				}

			}
			else
			{

				DisplayPopup = false;
				DisplaySecretPopup = false;

			}

			if ( Velocity.Length > 0f && lastStep >= 70 / Velocity.Length && GroundEntity != null )
			{

				string step = $"step{ Rand.Int( 5 ) }";
				Sound.FromEntity( step, this );
				lastStep = 0f;

			}

		}

		[ClientRpc]
		public void CreateViewModel()
		{

			ViewModel = new HarvestViewModel();
			ViewModel.Position = Position;
			ViewModel.Owner = Owner;
			ViewModel.EnableViewmodelRendering = true;
			ViewModel.SetModel( "models/viewmodel/viewmodel.vmdl" );

		}

		[ClientRpc]
		protected virtual void SetAnim( string name, bool state)
		{

			Host.AssertClient();

			(Local.Pawn as HarvestPlayer).ViewModel.SetAnimParameter( name, state );

		}

		[ConCmd.Server]
		public static void Harvest()
		{

			var ply = ConsoleSystem.Caller.Pawn as HarvestPlayer;

			ply.CatsHarvested++;
			ply.HasCat = false;
			ply.SetAnim( "finished", true );

			Sound.FromEntity( $"sad{ Rand.Int( 1 ) }", ply );
			Particles.Create( "particles/dollars.vpcf", ply.Position );

			if ( ply.CatsUprooted == 96 )
			{

				HarvestGame.EndGame( ply, ply.CatsHarvested );

			}

		}

		[ConCmd.Server]
		public static void Rescue()
		{

			var ply = ConsoleSystem.Caller.Pawn as HarvestPlayer;

			var cat = new WalkingCat
			{

				Position = ply.Position

			};

			Particles.Create( "particles/hearts.vpcf", cat.Position );

			ply.HasCat = false;
			ply.SetAnim( "finished", true );

			if ( ply.CatsUprooted == 96 )
			{

				HarvestGame.EndGame( ply, ply.CatsHarvested );

			}

		}

		public override void OnKilled()
		{
			//Don't die! wtf
		}

	}

	public class BalancedEndingCamera : CameraMode
	{

		float created = Time.Now;

		public override void Update()
		{

			Position = new Vector3( -300f + (Time.Now - created) * 50f, 0f, 300f + ( Time.Now - created ) * 50f );
			Rotation = Rotation.FromPitch( 80f );

			FieldOfView = 70f;

		}

	}

	public class PeacefulEndingCamera : CameraMode
	{

		float created = Time.Now;

		public override void Update()
		{

			Position = new Vector3( 0f, -200f + (Time.Now - created) * 30f, 100f + (Time.Now - created) * 10f );
			Rotation = Rotation.From( new Angles( 20f, 90f, 0f ) );

			FieldOfView = 70f;

		}

	}

}
