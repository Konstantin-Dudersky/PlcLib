using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Types;
using System;
using System.Collections.Generic;
using System.IO;


namespace TiaLibraryExport
{
    class Program
    {
        static void Main(string[] args)
        {

            using (TiaPortal tiaPortal = new TiaPortal())
            {
                Project project = null;

                try
                {
                    project = tiaPortal.Projects.Open(new FileInfo(Properties.Settings.Default.PROJECT_FILE));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                if (project != null)
                {
                    try
                    {
                        Console.WriteLine("Project opened");

                        foreach (var dev in project.Devices)
                        {
                            var plcSoftware = GetPlcSoftware(dev);

                            // очищаем папку
                            DirectoryInfo di = new DirectoryInfo(Properties.Settings.Default.EXPORT_BASE);
                            foreach (FileInfo file in di.GetFiles())
                                file.Delete();
                            foreach (DirectoryInfo dir in di.GetDirectories())
                                dir.Delete(true);

                            EnumerateAllBlocks(plcSoftware, Properties.Settings.Default.EXPORT_BASE);

                            EnumerateAllPlcDataTypes(plcSoftware, Properties.Settings.Default.EXPORT_BASE);
                        }
                    }
                    finally
                    {
                        project.Close();
                    }
                }
            }

            Console.WriteLine("Export complete, press any key to exit...");
            Console.ReadKey();
        }

        static private PlcSoftware GetPlcSoftware(Device device)
        {
            DeviceItemComposition deviceItemComposition = device.DeviceItems;
            foreach (DeviceItem deviceItem in deviceItemComposition)
            {
                SoftwareContainer softwareContainer = deviceItem.GetService<SoftwareContainer>();
                if (softwareContainer != null)
                {
                    Software softwareBase = softwareContainer.Software;
                    PlcSoftware plcSoftware = softwareBase as PlcSoftware;
                    return plcSoftware;
                }
            }
            return null;
        }

        //Enumerates all block user groups including sub groups
        private static void EnumerateAllBlocks(PlcSoftware plcsoftware, string path)
        {
            foreach (PlcBlockUserGroup blockUserGroup in plcsoftware.BlockGroup.Groups)
                EnumerateBlockGroup(blockUserGroup, plcsoftware, path);
        }

        private static void EnumerateBlockGroup(PlcBlockUserGroup blockUserGroup, PlcSoftware plcsoftware, string path)
        {
            if (blockUserGroup.Name.EndsWith("Test")) return;

            path += blockUserGroup.Name + "\\";
            Directory.CreateDirectory(path);

            foreach (var block in blockUserGroup.Blocks)
                plcsoftware.ExternalSourceGroup.GenerateSource(new List<PlcBlock> { block }, new FileInfo(path + block.Name + ".scl"));

            foreach (PlcBlockUserGroup subBlockUserGroup in blockUserGroup.Groups)
                EnumerateBlockGroup(subBlockUserGroup, plcsoftware, path);
        }

        private static void EnumerateAllPlcDataTypes(PlcSoftware plcsoftware, string path)
        {
            foreach (PlcTypeUserGroup typeGroup in plcsoftware.TypeGroup.Groups)
                EnumeratePlcDataTypeGroup(typeGroup, plcsoftware, path);
        }

        private static void EnumeratePlcDataTypeGroup(PlcTypeUserGroup typeGroup, PlcSoftware plcsoftware, string path)
        {
            if (typeGroup.Name.EndsWith("Test")) return;

            path += typeGroup.Name + "\\";
            Directory.CreateDirectory(path);

            foreach (var plcType in typeGroup.Types)
                plcsoftware.ExternalSourceGroup.GenerateSource(new List<PlcType> { plcType}, new FileInfo(path + plcType.Name + ".udt"));

            foreach (PlcTypeUserGroup typeSubGroup in typeGroup.Groups)
                EnumeratePlcDataTypeGroup(typeSubGroup, plcsoftware, path);
        }
    }
}