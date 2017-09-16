using System;
using System.Configuration;
using System.Threading;
using KopernikusWrapper;
using MyoSharp.Communication;
using MyoSharp.ConsoleSample;
using MyoSharp.ConsoleSample.Internal;
using MyoSharp.Device;
using MyoSharp.Exceptions;
using MyoSharp.Poses;

namespace MyoCar.MyoCar
{
    /// <summary>
    /// Main class for building the bridge between the Myo Controls and the KopernikusWrapper.
    /// </summary>
    internal class MyoCar
    {
        /// <summary>
        /// The controlled vehicle.
        /// </summary>
        private static Vehicle _vehicle;

        /// <summary>
        /// The timer for updating the vehicle's state.
        /// </summary>
        private static Timer _timer;

        /// <summary>
        /// The logger instance.
        /// </summary>
        private static Logger _logger;

        private static void Main()
        {
            _logger = new Logger();
            ConnectToVehicle();
            InitVehicle();
            _timer = CreateUpdateThread();
            AppDomain.CurrentDomain.ProcessExit += ProcessExitHandler;
            ConnectToMyos();
        }
        /// <summary>
        /// Establish a socket connection to the vehicle.
        /// Blocks until the connections has been established successfully.
        /// </summary>
        private static void ConnectToVehicle()
        {
            _vehicle = new Vehicle(ConfigurationManager.AppSettings["remoteHost"]);
            while (!_vehicle.Connected)
            {
            }
        }

        /// <summary>
        /// Assign initial state to the vehicle.
        /// This is, setting the gear direction to neutral.
        /// </summary>
        private static void InitVehicle()
        {
            _vehicle.SetGear(GearDirection.GEAR_DIRECTION_NEUTRAL);
        }

        /// <summary>
        /// Creates the thread for sending the commands to the vehicle.
        /// </summary>
        /// <returns>The resulting timer.</returns>
        private static Timer CreateUpdateThread()
        {
            return new Timer(TimerTask, null, 20, 20);
        }

        /// <summary>
        /// Defines the task for the update thread.
        /// </summary>
        /// <param name="stateObj"></param>
        private static void TimerTask(object stateObj)
        {
            _vehicle.Update();
        }

        /// <summary>
        /// Handler for process exit.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ProcessExitHandler(object sender, EventArgs e)
        {
            _timer.Dispose();
            _logger.Info("exit");
        }

        /// <summary>
        /// Connects to the Myos and register the neded handlers.
        /// </summary>
        private static void ConnectToMyos()
        {
            var channel = (Channel) CreateChannel();

            using (var hub = Hub.Create(channel))
            {
                // listen for when the Myo connects
                hub.MyoConnected += (sender, e) =>
                {
                    HandleMyoConnected(e);
                };

                // listen for when the Myo disconnects
                hub.MyoDisconnected += (sender, e) =>
                {
                    _logger.Info($"Oh no! It looks like {e.Myo.Arm} arm Myo has disconnected!");
                };

                // start listening for Myo data
                channel.StartListening();

                // wait on user input
                KeepAlive.UserInputLoop(hub);
            }
        }

        /// <summary>
        /// Creates the Myo channel.
        /// </summary>
        /// <returns>The Myo channel.</returns>
        private static IChannelListener CreateChannel()
        {
            var bridge = MyoErrorHandlerBridge.Create();
            var errorHandlerDriver = MyoErrorHandlerDriver.Create(bridge);
            var channelBridge = ChannelBridge.Create();
            var channelDriver = ChannelDriver.Create(channelBridge, errorHandlerDriver);
            return Channel.Create(channelDriver);
        }

        /// <summary>
        /// Handles the Myo connected event by assigning event handlers.
        /// </summary>
        /// <param name="myoEventArgs"></param>
        private static void HandleMyoConnected(MyoEventArgs myoEventArgs)
        {
            _logger.Info($"Myo {myoEventArgs.Myo.Handle} has connected!");
            myoEventArgs.Myo.Vibrate(VibrationType.Short);

            myoEventArgs.Myo.PoseChanged += PoseChanged;
            myoEventArgs.Myo.Locked += Locked;
            myoEventArgs.Myo.Unlocked += Unlocked;
        }

        /// <summary>
        /// Handles the pose changed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void PoseChanged(object sender, PoseEventArgs e)
        {
            if (e.Myo.Pose == Pose.WaveIn)
            {
                HandleWaveInPose();
            }

            if (e.Myo.Pose == Pose.WaveOut)
            {
                HandleWaveOutPose();
            }

            if (e.Myo.Pose == Pose.Fist && e.Myo.Arm == Arm.Left)
            {
                HandleLeftArmFistPose();
            }

            if (e.Myo.Pose == Pose.Fist && e.Myo.Arm == Arm.Right)
            {
                HandleRightArmFistPose();
            }
        }

        /// <summary>
        /// Handles the wave in position.
        /// </summary>
        private static void HandleWaveInPose()
        {
            var newGearDirection = GearDirection.GEAR_DIRECTION_FORWARD;
            if (_vehicle.Status.GearDirection == GearDirection.GEAR_DIRECTION_BACKWARD)
            {
                newGearDirection = GearDirection.GEAR_DIRECTION_NEUTRAL;
            }
            _vehicle.SetGear(newGearDirection);

            _logger.Info(_vehicle.Status.GearDirection.ToString());
        }

        /// <summary>
        /// Handles the wave out position.
        /// </summary>
        private static void HandleWaveOutPose()
        {
            var newGearDirection = GearDirection.GEAR_DIRECTION_BACKWARD;
            if (_vehicle.Status.GearDirection == GearDirection.GEAR_DIRECTION_FORWARD)
            {
                newGearDirection = GearDirection.GEAR_DIRECTION_NEUTRAL;
            }
            _vehicle.SetGear(newGearDirection);

            _logger.Info(_vehicle.Status.GearDirection.ToString());
        }

        /// <summary>
        /// Handles the fist pose for the left arm.
        /// </summary>
        private static void HandleLeftArmFistPose()
        {
            if (_vehicle.Status.TurnSignal == TurnSignal.TURN_SIGNAL_OFF)
            {
                _logger.Info("turn left on");
                _vehicle.SetTurnSignal(TurnSignal.TURN_SIGNAL_LEFT);
            }
            else
            {
                _logger.Info("turn left off");
                _vehicle.SetTurnSignal(TurnSignal.TURN_SIGNAL_OFF);
            }
        }

        /// <summary>
        /// Handles the fist pose for the right arm.
        /// </summary>
        private static void HandleRightArmFistPose()
        {
            if (_vehicle.Status.TurnSignal == TurnSignal.TURN_SIGNAL_OFF)
            {
                _logger.Info("turn right on");
                _vehicle.SetTurnSignal(TurnSignal.TURN_SIGNAL_RIGHT);
            }
            else
            {
                _logger.Info("turn right off");
                _vehicle.SetTurnSignal(TurnSignal.TURN_SIGNAL_OFF);
            }
        }

        /// <summary>
        /// Handles the locked event.
        /// It just unlocks the device, because otherwise controlling the vehicle gets really anoying.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Locked(object sender, MyoEventArgs e)
        {
            e.Myo.Unlock(UnlockType.Hold);
        }

        /// <summary>
        /// Handles the unlocked event by assigning the orientation handlers to the arms.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Unlocked(object sender, MyoEventArgs e)
        {
            if (e.Myo.Arm == Arm.Right)
            {
                e.Myo.OrientationDataAcquired += RightArmOrientationChanged;
            }
            if (e.Myo.Arm == Arm.Left)
            {
                e.Myo.OrientationDataAcquired += LeftArmOrientationChanged;
            }
        }

        /// <summary>
        /// Handles the orientation changed event of the right arm.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void RightArmOrientationChanged(object sender, OrientationDataEventArgs e)
        {
            var pitchDegree = e.Pitch * 180 / Constants.Pi;

            _logger.Debug($"Pitch percentage={pitchDegree}");

            var absAcceleartion = (float)(70f / 100f * Math.Abs(pitchDegree)) / 100f;

            if (pitchDegree > 0)
            {
                Accelerate(absAcceleartion);
            }
            else if (pitchDegree < 0)
            {
                Brake(absAcceleartion);
            }
        }

        /// <summary>
        /// Accelerates the vehicle.
        /// </summary>
        /// <param name="absAcceleartion"></param>
        private static void Accelerate(float absAcceleartion)
        {
            _vehicle.SetThrottle(absAcceleartion);
            _vehicle.SetBrake(0f);
        }

        /// <summary>
        /// Slows the vehicle down.
        /// </summary>
        /// <param name="absAcceleartion"></param>
        private static void Brake(float absAcceleartion)
        {
            _vehicle.SetBrake(absAcceleartion);
            _vehicle.SetThrottle(0f);
        }

        /// <summary>
        /// Handles the orientation changed event of the left arm.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void LeftArmOrientationChanged(object sender, OrientationDataEventArgs e)
        {
            var rollDegree = e.Roll * 180f / Constants.Pi;
            var rollPercentage = (float)(65f / 100f * rollDegree) / 100f;

            _logger.Debug($"Roll percentage={rollDegree}");

            _vehicle.SetSteeringAngle(rollPercentage * -1f);
        }
    }
}