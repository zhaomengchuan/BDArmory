using System;
using System.Collections.Generic;
using UnityEngine;

namespace BahaTurret
{
	public class MissileGuidance
	{
		
		public static Vector3 GetAirToGroundTarget(Vector3 targetPosition, Vessel missileVessel, float descentRatio)
		{
			Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(targetPosition).normalized;
			Vector3 surfacePos = missileVessel.transform.position - ((float)missileVessel.altitude*upDirection);
			Vector3 targetSurfacePos;

			targetSurfacePos = targetPosition;

			float distanceToTarget = Vector3.Distance(surfacePos, targetSurfacePos);
			
			if(missileVessel.srfSpeed < 75 && missileVessel.verticalSpeed < 10)//gain altitude if launching from stationary
			{
				return missileVessel.transform.position + (5*missileVessel.transform.forward) + (1 * upDirection);	
			}
			
			Vector3 finalTarget = targetPosition +(Mathf.Clamp((distanceToTarget-((float)missileVessel.srfSpeed*descentRatio))*0.22f, 0, 2000) * upDirection);
		

			return finalTarget;

		}

		public static Vector3 GetAirToAirTarget(Vector3 targetPosition, Vector3 targetVelocity, Vector3 targetAcceleration, Vessel missileVessel,  out float timeToImpact)
		{
			float leadTime = 0;
			float targetDistance = Vector3.Distance(targetPosition, missileVessel.transform.position);

			leadTime = (float)(1/((targetVelocity-missileVessel.srf_velocity).magnitude/targetDistance));
			timeToImpact = leadTime;
			leadTime = Mathf.Clamp(leadTime, 0f, 8f);
			Vector3 mTargetPosition = targetPosition + (targetVelocity*leadTime);

			if(targetDistance < 1600)
			{
				//mTargetPosition += (Vector3)targetAcceleration * 0.03f * Mathf.Pow(leadTime,2);
			}

		

			if(targetDistance > 500)
			{
				Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(targetPosition).normalized;
				float targetAltitude = FlightGlobals.getAltitudeAtPos(targetPosition);
				targetPosition += Mathf.Clamp((float)(targetAltitude-missileVessel.altitude)/6, -20, 1500)*upDirection;
			}

			Vector3 finalTarget = mTargetPosition;

			return finalTarget;
		}

		public static Vector3 GetAirToAirFireSolution(MissileLauncher missile, Vessel targetVessel)
		{
			if(!targetVessel)
			{
				return missile.transform.position + (missile.transform.forward*1000);
			}
			Vector3 targetPosition = targetVessel.transform.position;
			float leadTime = 0;
			float targetDistance = Vector3.Distance(targetVessel.transform.position, missile.transform.position);
 
			Vector3 simMissileVel = missile.optimumAirspeed * (targetPosition-missile.transform.position).normalized;
			leadTime = (float)(1/((targetVessel.srf_velocity-simMissileVel).magnitude/targetDistance));
			leadTime = Mathf.Clamp (leadTime, 0f, 8f);
			targetPosition = targetPosition + (targetVessel.srf_velocity*leadTime);

			if(targetVessel && targetDistance < 800)
			{
				targetPosition += (Vector3)targetVessel.acceleration * 0.05f * Mathf.Pow(leadTime,2);
			}
			
			return targetPosition;
		}

		public static Vector3 GetAirToAirFireSolution(MissileLauncher missile, Vector3 targetPosition, Vector3 targetVelocity)
		{
			float leadTime = 0;
			float targetDistance = Vector3.Distance(targetPosition, missile.transform.position);

			Vector3 simMissileVel = missile.optimumAirspeed * (targetPosition-missile.transform.position).normalized;
			leadTime = (1/((targetVelocity-simMissileVel).magnitude/targetDistance));
			leadTime = Mathf.Clamp (leadTime, 0f, 8f);

			targetPosition = targetPosition + (targetVelocity*leadTime);
			
			return targetPosition;
		}

		public static Vector3 GetCruiseTarget(Vector3 targetPosition, Vessel missileVessel, Vessel targetVessel, float radarAlt)
		{
			Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(missileVessel.GetWorldPos3D()).normalized;
			float currentRadarAlt = GetRadarAltitude(missileVessel);
			float distanceSqr = (targetPosition-(missileVessel.transform.position-(currentRadarAlt*upDirection))).sqrMagnitude;

			float agmThreshDist = 3500;

			Vector3 planarDirectionToTarget = Misc.ProjectOnPlane(targetPosition-missileVessel.transform.position, missileVessel.transform.position, upDirection).normalized;

			if(distanceSqr < agmThreshDist*agmThreshDist)
			{
				return GetAirToGroundTarget(targetPosition, missileVessel, 2.3f);
			}
			else
			{
				if(missileVessel.srfSpeed < 50 && missileVessel.verticalSpeed < 5) //gain altitude if launching from stationary
				{
					return missileVessel.transform.position + (5*missileVessel.transform.forward) + (40 * upDirection);	
				}

				Vector3 tRayDirection = (Misc.ProjectOnPlane(missileVessel.srf_velocity, missileVessel.transform.position, upDirection).normalized * 10) - (10*upDirection);
				Ray terrainRay = new Ray(missileVessel.transform.position, tRayDirection);
				RaycastHit rayHit;
				if(Physics.Raycast(terrainRay, out rayHit, 8000, 1<<15))
				{
					float detectedAlt = Vector3.Project(rayHit.point-missileVessel.transform.position, upDirection).magnitude;

					float error = Mathf.Min(detectedAlt, (float)missileVessel.altitude) - radarAlt;
					error = Mathf.Clamp(0.1f * error, -3, 3);

					return missileVessel.transform.position + (10*planarDirectionToTarget) - (error * upDirection);

				}
				else
				{
					float error = (float)missileVessel.altitude - radarAlt;
					error = Mathf.Clamp(0.1f * error, -3, 3);
					
					return missileVessel.transform.position + (10*planarDirectionToTarget) - (error * upDirection);	
				}

			}
		}

		public static Vector3 GetTerminalManeuveringTarget(Vector3 targetPosition, Vessel missileVessel, Vessel targetVessel)
		{
			Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(missileVessel.GetWorldPos3D()).normalized;
			Vector3 planarDirectionToTarget = Vector3.ProjectOnPlane(targetPosition-missileVessel.transform.position, upDirection).normalized;
			Vector3 crossAxis = Vector3.Cross(planarDirectionToTarget, upDirection).normalized;
			float sinAmplitude = Mathf.Clamp(Vector3.Distance(targetPosition, missileVessel.transform.position)-550, 0, 2500);
			Vector3 targetSin = (Mathf.Sin(2*Time.time) * sinAmplitude * crossAxis)+targetPosition;
			return GetAirToGroundTarget (targetSin, missileVessel, 6);
		}


		public static FloatCurve DefaultLiftCurve = null;
		public static FloatCurve DefaultDragCurve = null;
		public static Vector3 DoAeroForces(MissileLauncher ml, Vector3 targetPosition, float liftArea, float steerMult, Vector3 previousTorque, float maxTorque, float maxAoA)
		{
			if(DefaultLiftCurve == null)
			{
				DefaultLiftCurve = new FloatCurve();
				DefaultLiftCurve.Add(0, .1f);
				DefaultLiftCurve.Add(8, .25f);
				DefaultLiftCurve.Add(19, 1);
				DefaultLiftCurve.Add(23, .9f);
				DefaultLiftCurve.Add(29, 0.85f);
				DefaultLiftCurve.Add(35, 0.65f);
				DefaultLiftCurve.Add(65, .6f);
				DefaultLiftCurve.Add(90, .7f);
			}

			if(DefaultDragCurve == null)
			{
				DefaultDragCurve = new FloatCurve();
				DefaultDragCurve.Add(0, 0.0015f);
				DefaultDragCurve.Add(5, .0035f);
				DefaultDragCurve.Add(15, .015f);
				DefaultDragCurve.Add(29, .015f);
				DefaultDragCurve.Add(55, .3f);
				DefaultDragCurve.Add(90, .5f);
			}


			FloatCurve liftCurve = DefaultLiftCurve;
			FloatCurve dragCurve = DefaultDragCurve;

			return DoAeroForces(ml, targetPosition, liftArea, steerMult, previousTorque, maxTorque, maxAoA, liftCurve, dragCurve);
		}

		public static Vector3 DoAeroForces(MissileLauncher ml, Vector3 targetPosition, float liftArea, float steerMult, Vector3 previousTorque, float maxTorque, float maxAoA, FloatCurve liftCurve, FloatCurve dragCurve)
		{
			Rigidbody rb = ml.part.rb;
			double airDensity = ml.vessel.atmDensity;
			double airSpeed = ml.vessel.srfSpeed;
			Vector3d velocity = ml.vessel.srf_velocity;

			//temp values
			Vector3 CoL = new Vector3(0, 0, -1f);
			float liftMultiplier = BDArmorySettings.GLOBAL_LIFT_MULTIPLIER;
			float dragMultiplier = BDArmorySettings.GLOBAL_DRAG_MULTIPLIER;

			
			//lift
			float AoA = Mathf.Clamp(Vector3.Angle(ml.transform.forward, velocity.normalized), 0, 90);
			if(AoA > 0)
			{
				double liftForce = 0.5 * airDensity * Math.Pow(airSpeed, 2) * liftArea * liftMultiplier * liftCurve.Evaluate(AoA);
				Vector3 forceDirection = Vector3.ProjectOnPlane(-velocity, ml.transform.forward).normalized;
				rb.AddForceAtPosition((float)liftForce * forceDirection, ml.transform.TransformPoint(CoL));
			}

			//drag
			if(airSpeed > 0)
			{
				double dragForce = 0.5 * airDensity * Math.Pow(airSpeed, 2) * liftArea * dragMultiplier * dragCurve.Evaluate(AoA);
				rb.AddForceAtPosition((float)dragForce * -velocity.normalized, ml.transform.TransformPoint(CoL));
			}


			//guidance
			if(airSpeed > 1)
			{
				Vector3 targetDirection;
				float targetAngle;
				if(AoA < maxAoA)
				{
					targetDirection = (targetPosition - ml.transform.position);
					targetAngle = Vector3.Angle(velocity.normalized, targetDirection) * 4;
				}
				else
				{
					targetDirection = velocity.normalized;
					targetAngle = AoA;
				}

				Vector3 torqueDirection = -Vector3.Cross(targetDirection, velocity.normalized).normalized;
				torqueDirection = ml.transform.InverseTransformDirection(torqueDirection);

				float torque = Mathf.Clamp(targetAngle * steerMult, 0, maxTorque);
				Vector3 finalTorque = Vector3.ProjectOnPlane(Vector3.Lerp(previousTorque, torqueDirection*torque, 0.86f), Vector3.forward);
				
				rb.AddRelativeTorque(finalTorque);
				
				return finalTorque;
				
			}
			else
			{
				Vector3 finalTorque = Vector3.ProjectOnPlane(Vector3.Lerp(previousTorque, Vector3.zero, 0.25f), Vector3.forward);
				rb.AddRelativeTorque(finalTorque);
				return finalTorque;
			}
		}

		public static float GetRadarAltitude(Vessel vessel)
		{
			float radarAlt = Mathf.Clamp((float)(vessel.mainBody.GetAltitude(vessel.findWorldCenterOfMass())-vessel.terrainAltitude), 0, (float)vessel.altitude);
			return radarAlt;
		}

		public static float GetRaycastRadarAltitude(Vector3 position)
		{
			Vector3 upDirection = -FlightGlobals.getGeeForceAtPosition(position).normalized;
			Ray ray = new Ray(position, -upDirection);
			float rayDistance = FlightGlobals.getAltitudeAtPos(position);

			if(rayDistance < 0)
			{
				return 0;
			}

			RaycastHit rayHit;
			if(Physics.Raycast(ray, out rayHit, rayDistance, 1<<15)) 
			{
				return Vector3.Distance(position, rayHit.point);
			}
			else
			{
				return rayDistance;
			}
		}
	}
}

