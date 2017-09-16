using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KopernikusWrapper;
using MyoSharp.Communication;
using MyoSharp.ConsoleSample.Internal;
using MyoSharp.Device;
using MyoSharp.Exceptions;
using MyoSharp.Poses;

namespace MyoSharp.ConsoleSample
{
    class NoVehicle
    {
        private static void Main()
        {

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

                    Console.WriteLine(e.Myo.Arm);

                    e.Myo.PoseChanged += Myo_PoseChanged;
                    e.Myo.Locked += Myo_Locked;
                    e.Myo.Unlocked += Myo_Unlocked;

                };

                // listen for when the Myo disconnects
                hub.MyoDisconnected += (sender, e) =>
                {
                    Console.WriteLine("Oh no! It looks like {0} arm Myo has disconnected!", e.Myo.Arm);
                };

                // start listening for Myo data
                channel.StartListening();

                // wait on user input
                ConsoleHelper.UserInputLoop(hub);
            }
        }
        private static void Pitch(object sender, OrientationDataEventArgs e)
        {
            const float PI = (float)System.Math.PI;

            var pitchDegree = e.Pitch * 180 / PI;

            Console.WriteLine($"Pitch percentage={pitchDegree}");
        }

        private static void Roll(object sender, OrientationDataEventArgs e)
        {
            const float PI = (float)System.Math.PI;

            var rollDegree = e.Roll * 180f / PI;
            var rollPercentage = (float)((65f / 100f * rollDegree)) / 100f;

            Console.WriteLine($"Roll percentage={rollPercentage}");
        }

        private static void Myo_PoseChanged(object sender, PoseEventArgs e)
        {
            if (e.Myo.Pose == Pose.WaveIn)
            {
                Console.WriteLine("Gear up");
            }

            if (e.Myo.Pose == Pose.WaveOut)
            {
                Console.WriteLine("Gear down");
            }

            if (e.Myo.Pose == Pose.DoubleTap && e.Myo.Arm == Arm.Left)
            {
                Console.WriteLine("turn signal left");
            }

            if (e.Myo.Pose == Pose.DoubleTap && e.Myo.Arm == Arm.Right)
            {
                Console.WriteLine("turn signal right");
            }
        }

        private static void Myo_Unlocked(object sender, MyoEventArgs e)
        {
            if (e.Myo.Arm == Arm.Left)
            {
                e.Myo.OrientationDataAcquired += Roll;
            }
            if (e.Myo.Arm == Arm.Right)
            {
                e.Myo.OrientationDataAcquired += Pitch;
            }
        }

        private static void Myo_Locked(object sender, MyoEventArgs e)
        {
            e.Myo.Unlock(UnlockType.Hold);
        }
    }
}
