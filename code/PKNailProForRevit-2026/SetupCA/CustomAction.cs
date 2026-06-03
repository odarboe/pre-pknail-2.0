using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Deployment.WindowsInstaller;
using Autodesk.RevitAddIns;

namespace SetupCA
{
    public class CustomActions
    {    
        [CustomAction]
        public static ActionResult GetRevitEdition(Session session)
        {
            ActionResult retval;
            string logPrefix = @"Custom Action GetRevitEdition: ";
            session.Log(logPrefix + @"Begin");

            string revitEditionLabel;
            string fatalErrorMsg;
            List<string> logMsgs = new List<string>();
            bool isSucceeded = GetRevitEdition(out revitEditionLabel, out fatalErrorMsg, logMsgs);
            foreach (string logMsg in logMsgs)  { session.Log(logPrefix + logMsg); }

            if (isSucceeded)
            {
                if (revitEditionLabel.Length > 0)
                {
                    session[@"REVIT_EDITION_LABEL"] = revitEditionLabel;
                }
                retval = ActionResult.Success;
            }
            else
            {
                Record record = new Record();
                record.FormatString = fatalErrorMsg;
                session.Message(   InstallMessage.Error
                                | (InstallMessage)(System.Windows.Forms.MessageBoxIcon.Error)
                                | (InstallMessage)(System.Windows.Forms.MessageBoxButtons.OK), record);
                retval = ActionResult.Failure;
            }

            session.Log(logPrefix + @"End");
            return retval;
        }

        public static bool GetRevitEdition(out string editionLabel, out string fatalErrorMsg, List<string> logMsgs)
        {
            bool retval = true;
            editionLabel = @"";
            fatalErrorMsg = @"";
            string revitVersion = ProductInfo.ProductRevitVersion;

            try
            {
                RevitProduct revitProduct = matchingRevitProduct();

                if (revitProduct != null)
                {
                    logMsgs.Add(@"Detected installation of Revit " + revitVersion);
                    logMsgs.Add(@"Revit product type: " + revitProduct.Product);
                    string productEditionMsg = @"Revit product edition: ";
                    switch (revitProduct.Product)
                    {
                        case Autodesk.RevitAddIns.ProductType.Architecture:
                            editionLabel = @"RAC";
                            break;
                        case Autodesk.RevitAddIns.ProductType.Revit:
                        case Autodesk.RevitAddIns.ProductType.Unknown:
                            editionLabel = @"RVT";
                            break;
                        case Autodesk.RevitAddIns.ProductType.MEP:
                        case Autodesk.RevitAddIns.ProductType.Structure:
                            fatalErrorMsg = @"Autodesk Revit Architecture " + revitVersion + " is not installed on this computer, cannot continue";
                            logMsgs.Add(@"FATAL ERROR: " + fatalErrorMsg);
                            retval = false;
                            break;
                    }
                    if (retval) { logMsgs.Add(productEditionMsg + editionLabel); }
                }
                else
                {
                    fatalErrorMsg = @"Autodesk Revit " + revitVersion + @" is not installed on this computer, cannot continue";
                    logMsgs.Add(@"FATAL ERROR: " + fatalErrorMsg);
                    retval = false;
                }
            }
            catch (Exception e)
            {
                fatalErrorMsg = @"Caught exception in custom action GetRevitEdition(): " + e.Message;
                logMsgs.Add(fatalErrorMsg);
                retval = false;
            }

            return retval;
        }

        private static RevitProduct matchingRevitProduct()
        {
            string revitVersion = ProductInfo.ProductRevitVersion;
            int revitVersionAsInt = int.Parse(revitVersion);
            int matchingRevitVersionEnumVal = revitVersionAsInt - 2010;

            RevitProduct retval = null;
            List<RevitProduct> products = RevitProductUtility.GetAllInstalledRevitProducts();
            foreach (RevitProduct product in products)
            {
                if (product.Version == (RevitVersion)matchingRevitVersionEnumVal)
                { retval = product; break; }
            }
            return retval;
        }
    }    
}
