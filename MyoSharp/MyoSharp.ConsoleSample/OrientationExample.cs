using System;
using KopernikusWrapper;
using MyoSharp.Communication;
using MyoSharp.Device;
using MyoSharp.ConsoleSample.Internal;
using MyoSharp.Exceptions;

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

            vehicle = new Vehicle("localhost");
            while (vehicle.connectState == Vehicle.ConnectionState.AttemptingConnect)
            {
            }

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

        #region Event Handlers
        private static void Myo_OrientationDataAcquiredRight(object sender, OrientationDataEventArgs e)
        {
            const float PI = (float)System.Math.PI;

            var pitchDegree = e.Pitch * 180 / PI;

            Console.WriteLine($"Pitch percentage={pitchDegree}");

            if (pitchDegree > 0)
            {
              
                vehicle.SetThrottle((float) (((70f / 100f) * pitchDegree)) / 100f);
                vehicle.SetBrake(0f);
            }
            else if (pitchDegree < 0)
            {
                Console.WriteLine("lower 0");
                vehicle.SetBrake((float) (((70f / 100f) * System.Math.Abs(pitchDegree)) / 100f));
                vehicle.SetThrottle(0f);
            }
            vehicle.Update();
        }

        private static void Myo_OrientationDataAcquiredLeft(object sender, OrientationDataEventArgs e)
        {
            const float PI = (float)System.Math.PI;

            var rollDegree = e.Roll * 180f / PI;
            var rollPercentage = (float)((65f / 100f * rollDegree)) / 100f;

            Console.WriteLine($"Roll percentage={rollDegree}");

            vehicle.SetSteeringAngle(rollPercentage * -1f);
            vehicle.Update();
        }

        private static void Myo_PoseChanged(object sender, PoseEventArgs e)
        {
            Console.WriteLine("{0} arm Myo detected {1} pose!", e.Myo.Arm, e.Myo.Pose);
        }

        private static void Myo_Unlocked(object sender, MyoEventArgs e)
        {
            if (e.Myo.Arm == Arm.Right)
            {
                e.Myo.OrientationDataAcquired += Myo_OrientationDataAcquiredRight;
            }
            if (e.Myo.Arm == Arm.Left)
            {
                e.Myo.OrientationDataAcquired += Myo_OrientationDataAcquiredLeft;
            }
        }

        private static void Myo_Locked(object sender, MyoEventArgs e)
        {
            Console.WriteLine("{0} arm Myo has locked!", e.Myo.Arm);
        }
        #endregion
    }
}