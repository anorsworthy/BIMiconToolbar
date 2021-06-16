﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;

namespace BIMiconToolbar.NumberBySpline
{
    [TransactionAttribute(TransactionMode.Manual)]
    class NumberBySpline : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
			Document doc = commandData.Application.ActiveUIDocument.Document;

            // Call WPF for user input
            using (NumberBySplineWPF customWindow = new NumberBySplineWPF(commandData))
            {
                // Revit application as window's owner
                System.Windows.Interop.WindowInteropHelper helper = new System.Windows.Interop.WindowInteropHelper(customWindow);
                helper.Owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

                customWindow.ShowDialog();
                ElementId curveId = customWindow.CurveId;

                if (customWindow.Cancel == false && curveId != null)
                {
                    CurveElement eCurve = doc.GetElement(curveId) as CurveElement;
                    Curve curve = eCurve.GeometryCurve as Curve;

                    XYZ[] points = Helpers.HelpersGeometry.DivideEquallySpline(curve, 1000);

                    // Retrieve elements of selected category
                    Category cat = customWindow.SelectedComboItemCategories.Tag as Category;
                    Parameter selParameter = customWindow.SelectedComboItemParameters.Tag as Parameter;
                    string startNumber = customWindow.StartNumber;
                    string prefix = customWindow.Prefix;

                    FilteredElementCollector collectElements = new FilteredElementCollector(doc).OfCategoryId(cat.Id).WhereElementIsNotElementType();

                    // Create two list that contains all selected elements
                    List<Element> selElements = new List<Element>();
                    List<Element> selElementsCopy = new List<Element>();

                    // If selected elements don't have parameter level
                    if (customWindow.levelDisplay)
                    {
                        // Retrieve user level selection
                        Level level = customWindow.SelectedComboItemLevels.Tag as Level;
                        ElementLevelFilter levelFilter = new ElementLevelFilter(level.Id);

                        // Collect elements that match user selected level
                        foreach (Element element in collectElements)
                        {
                            if (element.LevelId == level.Id)
                            {
                                selElements.Add(element);
                                selElementsCopy.Add(element);
                            }
                        }
                    }
                    // Convert collector to list of elements if no level filter is required
                    else
                    {
                        selElements = (List<Element>)collectElements.ToElements();
                        selElementsCopy = (List<Element>)collectElements.ToElements();
                    }

                    // Renumber selected elements
                    using (Transaction tx = new Transaction(doc))
                    {
                        tx.Start("Draw Curves at Points");

                        int number = int.Parse(startNumber);

                        // Loop through each point to check if it is inside the selected elements
                        foreach (XYZ point in points)
                        {
                            // Inner loop to check point is inside the element, if so, remove element from list and proceed until end of list
                            for (int j = 0; j < selElementsCopy.Count; j++)
                            {
                                // Retrieve bounding box of element
                                BoundingBoxXYZ bBox = selElementsCopy[j].get_BoundingBox(null);
                                // Try get bounding box in active view if model geometry one is empty
                                if (bBox == null)
                                {
                                    bBox = selElementsCopy[j].get_BoundingBox(doc.ActiveView);
                                }
                                // Element bounding box is valid
                                if (bBox != null)
                                {
                                    int intersResult = Helpers.HelpersGeometry.IsPointInsideRectangle(point, bBox.Min, bBox.Max);
                                    Parameter param = selElementsCopy[j].LookupParameter(selParameter.Definition.Name);
                                    bool isParamReadOnly = param.IsReadOnly;

                                    if (intersResult != 0 && isParamReadOnly == false)
                                    {
                                        param.Set(prefix + number.ToString());
                                        selElementsCopy.Remove(selElementsCopy[j]);

                                        number++;
                                        break;
                                    }
                                    else if (isParamReadOnly)
                                    {
                                        Helpers.MessageWindows.AlertMessage("Warning", "Parameter is read only. Please select another parameter.");
                                        tx.RollBack();
                                        // Stop the program
                                        return Result.Failed;
                                    }
                                }
                            }
                        }
                        tx.Commit();
                    }
                    return Result.Succeeded;
                }
                return Result.Cancelled;
            }
        }
    }
}
