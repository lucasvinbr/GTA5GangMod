using GTA.Native;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Xml.Serialization;


namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// This script controls all the saving and loading procedures called by the other scripts from this mod.
    /// </summary>
    public static class PedExtension
    {
        public static bool IsUsingAnyVehicleWeapon(this Ped ped)
        {
            return ped.VehicleWeapon != VehicleWeaponHash.Invalid;
        }

    }
}