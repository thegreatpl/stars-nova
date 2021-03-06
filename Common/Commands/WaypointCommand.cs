﻿ #region Copyright Notice
 // ============================================================================
 // Copyright (C) 2011-2012 The Stars-Nova Project
 //
 // This file is part of Stars-Nova.
 // See <http://sourceforge.net/projects/stars-nova/>;.
 //
 // This program is free software; you can redistribute it and/or modify
 // it under the terms of the GNU General Public License version 2 as
 // published by the Free Software Foundation.
 //
 // This program is distributed in the hope that it will be useful,
 // but WITHOUT ANY WARRANTY; without even the implied warranty of
 // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 // GNU General Public License for more details.
 //
 // You should have received a copy of the GNU General Public License
 // along with this program. If not, see <http://www.gnu.org/licenses/>;
 // ===========================================================================
 #endregion
 
namespace Nova.Common.Commands
{
    using System;
    using System.Xml;
    
    using Nova.Common.Waypoints;
    
    /// <summary>
    /// Description of WaypointCommand.
    /// </summary>
    public class WaypointCommand : ICommand
    {        
        public Waypoint Waypoint
        {
            get;
            set;
        }
        
        public int Index
        {
            get;
            set;
        }
        
        public CommandMode Mode
        {
            get;
            set;
        }
        
        public long FleetKey
        {
            get;
            set;
        }
        
        
        /// <summary>
        /// Default Constructor.
        /// </summary>
        public WaypointCommand()
        {
            Waypoint = new Waypoint();
            Mode = CommandMode.Add;
            FleetKey = Global.None;
            Index = 0;
        }
        
        /// <summary>
        /// Deletion Constructor. Waypoint is null as it is not used.
        /// </summary>
        /// <param name="mode">CommandMode. Should be Delete.</param>
        /// <param name="fleetKey">Fleet key whose waypoints will be affected.</param>
        /// <param name="index">Waypoint Index to delete.</param>
        public WaypointCommand(CommandMode mode, long fleetKey = Global.None, int index = 0)
        {
            Waypoint = null;
            Mode = mode;
            FleetKey = fleetKey;
            Index = index;
        }
        
        /// <summary>
        /// Add/Edit Constructor.
        /// </summary>
        /// <param name="mode">CommandMode.</param>
        /// <param name="waypoint">New Waypoint to create or that will replace an existing one.</param>
        /// <param name="fleetKey">Fleet key whose waypoints will be affected.</param>
        /// <param name="index">Waypoint Index to edit, or where to insert.</param>
        public WaypointCommand(CommandMode mode, Waypoint waypoint, long fleetKey = Global.None, int index = 0)
        {
            Waypoint = waypoint;
            Mode = mode;
            FleetKey = fleetKey;
            Index = index;
        }
                
        
        /// <summary>
        /// Load from XML: Initializing constructor from an XML node.
        /// </summary>
        /// <param name="node">An <see cref="XmlNode"/> within
        /// a Nova component definition file (xml document).
        /// </param>
        public WaypointCommand(XmlNode node)
        {
            XmlNode subnode = node.FirstChild;
            
            while (subnode != null)
            {
                switch (subnode.Name.ToLower())
                {
                    case "fleetkey":
                      FleetKey = long.Parse(subnode.FirstChild.Value, System.Globalization.NumberStyles.HexNumber);
                    break;
                    case "mode":
                        Mode = (CommandMode)Enum.Parse(typeof(CommandMode), subnode.FirstChild.Value);
                    break;
                    case "index":
                        Index = int.Parse(subnode.FirstChild.Value, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                    case "waypoint":
                        Waypoint = new Waypoint(subnode);
                    break;
                }
            
                subnode = subnode.NextSibling;
            }   
        }
        
        
        /// <inheritdoc />
        public bool IsValid(EmpireData empire)
        {
            if (!empire.OwnedFleets.ContainsKey(FleetKey))
            {
                return false;
            }
            
            return true;
        }
        
        
        /// <inheritdoc />
        public void ApplyToState(EmpireData empire)
        {
            switch (Mode)
            {
                case CommandMode.Add:
                    empire.OwnedFleets[FleetKey].Waypoints.Add(Waypoint);
                break;
                case CommandMode.Delete:
                    empire.OwnedFleets[FleetKey].Waypoints.RemoveAt(Index);
                break;
                case CommandMode.Edit:
                    empire.OwnedFleets[FleetKey].Waypoints.RemoveAt(Index);
                    empire.OwnedFleets[FleetKey].Waypoints.Insert(Index, Waypoint);
                break;
            }
        }
        
        
        /// <summary>
        /// Save: Serialize this property to an <see cref="XmlElement"/>.
        /// </summary>
        /// <param name="xmldoc">The parent <see cref="XmlDocument"/>.</param>
        /// <returns>An <see cref="XmlElement"/> representation of the Command.</returns>
        public XmlElement ToXml(XmlDocument xmldoc)
        {
            XmlElement xmlelCom = xmldoc.CreateElement("Command");
            xmlelCom.SetAttribute("Type", "Waypoint");
            Global.SaveData(xmldoc, xmlelCom, "Mode", Mode.ToString());
            Global.SaveData(xmldoc, xmlelCom, "FleetKey", FleetKey.ToString("X"));
            Global.SaveData(xmldoc, xmlelCom, "Index", Index.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (Waypoint != null)
            {
                xmlelCom.AppendChild(Waypoint.ToXml(xmldoc));
            }
            
            return xmlelCom;    
        }
    }
}
