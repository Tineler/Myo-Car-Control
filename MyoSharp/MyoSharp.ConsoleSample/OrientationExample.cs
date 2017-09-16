using System;
using System.Configuration;
using KopernikusWrapper;
using MyoSharp.Communication;
using MyoSharp.Device;
using MyoSharp.ConsoleSample.Internal;
using MyoSharp.Exceptions;
using MyoSharp.Poses;

namespace MyoSharp.ConsoleSample
{

    /// <summary>
    /// This example will show you how to hook onto the orientation events on
    /// the Myo and pull roll, pitch and yaw values from it. In this example the 
    /// raw vectors from the orientation event args are converted to roll, pitch and yaw
    /// on a scale from 0 to 9, depending on the position of the myo
    /// </summary>
    /// <remarks>
    /// Not sure how to use this example?
    /// - Open Visual Studio
    /// - Go to the solution explorer
    /// - Find the project that this file is contained within
    /// - Right click on the project in the solution explorer, go to "properties"
    /// - Go to the "Application" tab
    /// - Under "Startup object" pick this example from the list
    /// - Hit F5 and you should be good to go!
    /// </remarks>
    internal class OrientationExample
    {
        private static Vehicle vehicle;
        #region Methods
        private static void Main()
        {

            vehicle = new Vehicle(ConfigurationManager.AppSettings["remoteHost"]);
            while (!vehicle.Connected)
            {
            }
            System.Threading.TimerCallback TimerDelegate = new System.Threading.TimerCallback(TimerTask);

            // Create a timer that calls a procedure every 2 seconds.
            // Note: There is no Start method; the timer starts running as soon as 
            // the instance is created.
            System.Threading.Timer TimerItem = new System.Threading.Timer(TimerDelegate, null, 20, 20);
            vehicle.SetGear(GearDirection.GEAR_DIRECTION_NEUTRAL);
            //vehicle.Update();

            // create a hub that will manage Myo devices for us
            using (var channel = Channel.Create(
                ChannelDriver.Create(ChannelBridge.Create(),
                MyoErrorHandlerDriver.Create(MyoErrorHandlerBridge.Create()))))
            using (var hub = Hub.Create(channel))
            {
                // listen for when the Myo connects
                hub.MyoConnected += (sender, e) =>
                {
                    Console.WriteLine("Myo {0} has connected!", e.Myo.Handle);
                    e.Myo.Vibrate(VibrationType.Short);

                    e.Myo.PoseChanged += Myo_PoseChanged;
                    e.Myo.Locked += Myo_Locked;
                    e.Myo.Unlocked += Myo_Unlocked;

                };

                // listen for when the Myo disconnects
                hub.MyoDisconnected += (sender, e) =>
                {
                    Console.WriteLine("Oh no! It looks like {0} arm Myo has disconnected!", e.Myo.Arm);
                    e.Myo.OrientationDataAcquired -= Myo_OrientationDataAcquired;
                };

                // start listening for Myo data
                channel.StartListening();

                // wait on user input
                ConsoleHelper.UserInputLoop(hub);
            }
        }
        #endregion

        private static void TimerTask(object StateObj)
        {
            vehicle.Update();
        }

        #region Event Handlers
        private static void Myo_OrientationDataAcquired(object sender, OrientationDataEventArgs e)
        {
            const float PI = (float)System.Math.PI;

            var pitchDegree = e.Pitch * 180 / PI;

            //Console.WriteLine($"Pitch percentage={pitchDegree}");

            if (pitchDegree > 0)
            {
              
                vehicle.SetThrottle((float) (((70f / 100f) * pitchDegree)) / 100f);
                vehicle.SetBrake(0f);
            }
            else if (pitchDegree < 0)
            {
                vehicle.SetBrake((float) (((70f / 100f) * System.Math.Abs(pitchDegree)) / 100f));
                vehicle.SetThrottle(0f);
            }
            //vehicle.Update();
        }

        private static void Myo_OrientationDataAcquiredLeft(object sender, OrientationDataEventArgs e)
        {
            const float PI = (float)System.Math.PI;

            var rollDegree = e.Roll * 180f / PI;
            var rollPercentage = (float)((65f / 100f * rollDegree)) / 100f;

            //Console.WriteLine($"Roll percentage={rollDegree}");

            vehicle.SetSteeringAngle(rollPercentage * -1f);
            //vehicle.Update();
        }

        private static void Myo_PoseChanged(object sender, PoseEventArgs e)
        {
            if (e.Myo.Pose == Pose.WaveIn)
            {
                switch (vehicle.Status.GearDirection)
                {
                    case GearDirection.GEAR_DIRECTION_NEUTRAL:
                    case GearDirection.GEAR_DIRECTION_UNKNOWN:
                        vehicle.SetGear(GearDirection.GEAR_DIRECTION_FORWARD);
                        //vehicle.Update();
                        break;
                    case GearDirection.GEAR_DIRECTION_BACKWARD:
                        vehicle.SetGear(GearDirection.GEAR_DIRECTION_NEUTRAL);
                        //vehicle.Update();
                        break;
                }

                Console.WriteLine(vehicle.Status.GearDirection);
            }

            if (e.Myo.Pose == Pose.WaveOut)
            {
                switch (vehicle.Status.GearDirection)
                {
                    case GearDirection.GEAR_DIRECTION_NEUTRAL:
                    case GearDirection.GEAR_DIRECTION_UNKNOWN:
                        vehicle.SetGear(GearDirection.GEAR_DIRECTION_BACKWARD);
                        //vehicle.Update();
                        break;
                    case GearDirection.GEAR_DIRECTION_FORWARD:
                        vehicle.SetGear(GearDirection.GEAR_DIRECTION_NEUTRAL);
                        //vehicle.Update();
                        break;
                }

                Console.WriteLine(vehicle.Status.GearDirection);
            }

            if (e.Myo.Pose == Pose.Fist && e.Myo.Arm == Arm.Left)
            {
                if (vehicle.Status.TurnSignal == TurnSignal.TURN_SIGNAL_OFF)
                {
                    Console.WriteLine("turn left on");
                    vehicle.SetTurnSignal(TurnSignal.TURN_SIGNAL_LEFT);
                }
                else
                {
                    Console.WriteLine("turn left off");
                    vehicle.SetTurnSignal(TurnSignal.TURN_SIGNAL_OFF);
                }
            }

            if (e.Myo.Pose == Pose.Fist && e.Myo.Arm == Arm.Right)
            {
                if (vehicle.Status.TurnSignal == TurnSignal.TURN_SIGNAL_OFF)
                {
                    Console.WriteLine("turn right on");
                    vehicle.SetTurnSignal(TurnSignal.TURN_SIGNAL_RIGHT);
                }
                else
                {
                    Console.WriteLine("turn right off");
                    vehicle.SetTurnSignal(TurnSignal.TURN_SIGNAL_OFF);
                }
            }
        }

        private static void Myo_Unlocked(object sender, MyoEventArgs e)
        {
            if (e.Myo.Arm == Arm.Right)
            {
                e.Myo.OrientationDataAcquired += Myo_OrientationDataAcquired;
            }
            if (e.Myo.Arm == Arm.Left)
            {
                e.Myo.OrientationDataAcquired += Myo_OrientationDataAcquiredLeft;
            }
        }

        private static void Myo_Locked(object sender, MyoEventArgs e)
        {
            e.Myo.Unlock(UnlockType.Hold);
        }
        #endregion
    }
}