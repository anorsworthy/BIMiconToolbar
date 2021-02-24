﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BIMiconToolbar.Helpers.Browser;
using System.IO;

namespace BIMiconToolbar.FilesRename
{
    [TransactionAttribute(TransactionMode.Manual)]
    class FilesRename : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            using (BrowserWindow browserWindow = new BrowserWindow())
            {
                browserWindow.ShowDialog();

                string selectedPath = browserWindow.selectedPath;
                //string[] fullPath = new string[] { @"C:\Users\BIMicon\Desktop\test\BIMicon Content" };

                // Check that path is not empty and path is a folder
                if (selectedPath == null || !Directory.Exists(selectedPath))
                {
                    TaskDialog.Show("Warning", "No folder has been selected");
                    return Result.Cancelled;
                }
                // Show next window for user input
                else
                {
                    Helpers.HelpersDirectory.MoveDirectory(new string[]{ "C:\\Users\\BIMicon\\Desktop\\test\\BIMicon Content" },
                        new string[]{ "C:\\Users\\BIMicon\\Desktop\\test\\123" });
                    //Directory.Move("C:\\Users\\BIMicon\\Desktop\\test\\BIMicon Content", "C:\\Users\\BIMicon\\Desktop\\test\\123");

                    TaskDialog.Show("Warning","WIP");
                }
            }

            return Result.Succeeded;
        }
    }
}
