using KopernikusWrapper;

namespace MyoCarControl
{
    class Program
    {
        static void Main(string[] args)
        {
            var vehicle = new Vehicle("localhost");
            while (vehicle.connectState == Vehicle.ConnectionState.AttemptingConnect)
            {
            }

            while (true)
            {
                vehicle.SetThrottle(0.8f);
                vehicle.SetSteeringAngle(0.5f);
                vehicle.Update();
            }
        }
    }
}
