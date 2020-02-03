using System;
using System.Threading.Tasks;

namespace DpsManagement
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var dpsEnrollmentGroup = new DpsEnrollmentGroup();
            await dpsEnrollmentGroup.CreateDpsEnrollmentGroupAsync();

            var dpsRegisterDevice = new DpsRegisterDevice();
            await dpsRegisterDevice.RegisterDeviceAsync();
        }
    }
}
