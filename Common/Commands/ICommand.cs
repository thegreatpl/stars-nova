﻿ #region Copyright Notice
 // ============================================================================
 // Copyright (C) 2008 Ken Reed
 // Copyright (C) 2011 The Stars-Nova Project
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

    /// <summary>
    /// Description of ICommand.
    /// </summary>
    /// <remarks>
    /// Note that OrderReader.cs ReadPlayersTurn() must be modified when new commands are added to recognize the command type in the xml.
    /// </remarks>
    public interface ICommand
    {
        bool IsValid(EmpireData empire);
        
        void ApplyToState(EmpireData empire);
        
        XmlElement ToXml(XmlDocument xmldoc);
    }
    
    public enum CommandMode
    {
        Add,
        Edit,
        Delete
    }
}
