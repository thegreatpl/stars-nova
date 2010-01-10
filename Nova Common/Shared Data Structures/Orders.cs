// ============================================================================
// Nova. (c) 2008 Ken Reed
//
// This module contains the data that is generated by the Nova GUI and passed
// to the Nova Console so that it can generate the turn for the next year. These
// are the orders sent to the console/server.
//
// This is free software. You can redistribute it and/or modify it under the
// terms of the GNU General Public License version 2 as published by the Free
// Software Foundation.
// ============================================================================

using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System;
using System.IO;
using System.IO.Compression;
using System.Xml;


// ============================================================================
// Definition of the orders that are created by the Nova GUI and read by the
// Nova Console.
// ============================================================================

namespace NovaCommon
{
   [Serializable]
   public sealed class Orders
   {
      public ArrayList   RaceFleets      = new ArrayList(); // For fleet orders
      public ArrayList   RaceDesigns     = new ArrayList(); // For any new designs
      public ArrayList   RaceStars       = new ArrayList(); // For production queues
      public ArrayList   DeletedFleets   = new ArrayList(); // To delete fleets
      public ArrayList   DeletedDesigns  = new ArrayList(); // To delete designs
      public RaceData    PlayerData      = new RaceData();  // Player relations, battle orders & turn # (turn # so we can check these orders are for the right year.)
      public int         TechLevel       = 0;               // FIXME (priority 4): should send our research orders; server should control actual player tech level ??? what does this int mean? it is not a TechLevel type.

       // private data
       private static BinaryFormatter Formatter = new BinaryFormatter();

       /// <summary>
       /// default constructor
       /// </summary>
       public Orders() { }

       public Orders(XmlNode xmlnode)
       {
           // temporary variables for reading in designs, fleets, stars
           Design design = null; 
           ShipDesign shipDesign = null;
           Fleet fleet = null;
           Star star = null;

           // Read the node
           while (xmlnode != null)
           {
               try
               {
                   switch (xmlnode.Name.ToLower())
                   {
                       case "root": xmlnode = xmlnode.FirstChild; continue;
                       case "orders": xmlnode = xmlnode.FirstChild; continue;
                       case "techlevel": TechLevel = int.Parse(((XmlText)xmlnode.FirstChild).Value, System.Globalization.CultureInfo.InvariantCulture); break;

                       case "design":
                           string type = xmlnode.FirstChild.SelectSingleNode("Type").Value;
                           if (type.ToLower() == "ship" || type == "starbase")
                           {
                               shipDesign = new ShipDesign(xmlnode.FirstChild);
                               RaceDesigns.Add(shipDesign);
                           }
                           else
                           {
                               design = new Design(xmlnode.FirstChild);
                               RaceDesigns.Add(design);
                           }
                           break;

                       case "star":
                           star = new Star(xmlnode.FirstChild);
                           RaceStars.Add(star);
                           break;

                       case "fleet":
                           fleet = new Fleet(xmlnode.FirstChild);
                           RaceFleets.Add(fleet);
                           break;

                       case "racedata":
                           PlayerData = new RaceData(xmlnode.FirstChild);
                           break;

                       default: break;
                   }

               }
               catch
               {
                   // If there are blank entries null reference exemptions will be raised here. It is safe to ignore them.
               }
               xmlnode = xmlnode.NextSibling;
           }
       }

       /// <summary>
       /// Write out the orders file using binary serialization
       /// </summary>
       public void ToBinary(string ordersFileName)
       {
           FileStream ordersFile = new FileStream(ordersFileName, FileMode.Create);
           Formatter.Serialize(ordersFile, this);
           ordersFile.Close();
       }

       /// <summary>
       /// Write out the orders using xml format
       /// </summary>
       /// <param name="ordersFileName">The path&filename to save the orders too.</param>
       public void ToXml(string ordersFileName)
       {
           FileStream ordersFile = new FileStream(ordersFileName, FileMode.Create);
           GZipStream compressionStream = new GZipStream(ordersFile, CompressionMode.Compress);

           // Setup the XML document
           XmlDocument xmldoc;
           XmlNode xmlnode;
           XmlElement xmlRoot;
           xmldoc = new XmlDocument();
           // TODO (priority 4) - add the encoding attribute like UTF-8 ???
           xmlnode = xmldoc.CreateNode(XmlNodeType.XmlDeclaration, "", "");
           xmldoc.AppendChild(xmlnode);
           xmlRoot = xmldoc.CreateElement("", "ROOT", "");
           xmldoc.AppendChild(xmlRoot);

           // add the orders to the document
           XmlElement xmlelOrders = xmldoc.CreateElement("Orders");
           xmlRoot.AppendChild(xmlelOrders);

           // Place the turn year first, so it can be determined quickly
           Global.SaveData(xmldoc, xmlelOrders, "Turn", PlayerData.TurnYear.ToString(System.Globalization.CultureInfo.InvariantCulture));

           // Store the fleets, to pass on fleet orders
           foreach (Fleet fleet in RaceFleets)
           {
               xmlelOrders.AppendChild(fleet.ToXml(xmldoc));
           }
           // store the designs, for any new designs
           foreach (Design design in RaceDesigns)
           {
               if (design.Type == "Ship" || design.Type == "Starbase")
                   xmlelOrders.AppendChild(((ShipDesign)design).ToXml(xmldoc));
               else
                   xmlelOrders.AppendChild(design.ToXml(xmldoc));
           }
           // store the stars, so we can pass production orders
           foreach (Star star in RaceStars)
           {
               xmlelOrders.AppendChild(star.ToXml(xmldoc));
           }
           foreach (Fleet fleet in DeletedFleets)
           {
               xmlelOrders.AppendChild(fleet.ToXml(xmldoc)); // TODO (priority 5) how do we tell these from the current fleets ???
           }
           foreach (Design design in DeletedDesigns)
           {
               // TODO (priority 5) how do we tell these from the current designs ???
               if (design.Type == "Ship" || design.Type == "Starbase")
                   xmlelOrders.AppendChild(((ShipDesign)design).ToXml(xmldoc));
               else
                   xmlelOrders.AppendChild(design.ToXml(xmldoc)); 
               
           }
           xmlelOrders.AppendChild(PlayerData.ToXml(xmldoc));
           Global.SaveData(xmldoc, xmlelOrders, "TechLevel", TechLevel.ToString(System.Globalization.CultureInfo.InvariantCulture));
           

           // You can comment/uncomment the following lines to turn compression on/off if you are doing a lot of 
           // manual inspection of the save file. Generally though it can be opened by any archiving tool that
           // reads gzip format.
#if (DEBUG)
           xmldoc.Save(ordersFile);                                           //  not compressed
#else
           xmldoc.Save(compressionStream); compressionStream.Close();    //   compressed 
#endif

           ordersFile.Close();

           Report.Information("Orders saved.");

       }
   }

}
