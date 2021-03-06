﻿using Nova.Common;
#region Copyright Notice
// ============================================================================
// Copyright (C) 2009-2012 The Stars-Nova Project
//
// This file is part of Stars-Nova.
// See <http://sourceforge.net/projects/stars-nova/>.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License version 2 as
// published by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>
// ===========================================================================
#endregion

#region Module Description
// ===========================================================================
// Unit tests for ServerState.
// ===========================================================================
#endregion



namespace Nova.Tests.IntegrationTests
{
    using System.Xml;
    
    using Nova.Common.DataStructures;
    using Nova.Server;
    using NUnit.Framework;

    /// ----------------------------------------------------------------------------
    /// <Summary>
    /// Unit tests for ServerState. 
    /// </Summary>
    /// ----------------------------------------------------------------------------
    [TestFixture]
    public class ServerStateTest
    {
        private const int Player1Id = 1;
        private const int Player2Id = 2;
        private const int Player3Id = 3;

        /// <Summary>
        /// Test the (de)serialising of the ServerState.
        /// 
        /// TODO Magic numbers (2101, 2102 is a magic number) in tests is not recommended.
        /// </Summary>
        [Test]
        public void SerialisationTest()
        {
            ServerData serverState = new ServerData();
            // Setup the initial state
            serverState.TurnYear = 2101;
            serverState.GameInProgress = true;
            serverState.GameFolder = "dummy_value";
            serverState.StatePathName = "unit_test.sstate";
            serverState.AllTechLevels[1] = 10;
            serverState.AllTechLevels[2] = 5;
            Common.Fleet fleet1 = new Nova.Common.Fleet("foofleet", Player1Id, 1, new Nova.Common.DataStructures.NovaPoint(0, 0));
            Common.Fleet fleet2 = new Nova.Common.Fleet("barfleet", Player1Id, 2, new Nova.Common.DataStructures.NovaPoint(0, 0));
            EmpireData empire1 = new EmpireData();
            EmpireData empire2 = new EmpireData();
            empire1.Id = Player1Id;
            empire2.Id = Player2Id;
            serverState.AllEmpires.Add(Player1Id, empire1);
            serverState.AllEmpires.Add(Player2Id, empire2);
            serverState.AllEmpires[Player1Id].OwnedFleets[fleet1.Key] = fleet1;
            serverState.AllEmpires[Player2Id].OwnedFleets[fleet2.Key] =  fleet2;

            // Serialize
            serverState.Save();

            // change the value to ensure it is restored.
            serverState.TurnYear = 2102;
            serverState.GameInProgress = false;
            serverState.GameFolder = "foo_bar";
            serverState.AllTechLevels[1] = 2;
            serverState.AllTechLevels[2] = 7;
            fleet1 = new Nova.Common.Fleet("fleetfoo", Player1Id, 1, new Nova.Common.DataStructures.NovaPoint(0, 0));
            fleet2 = new Nova.Common.Fleet("fleetbar", Player1Id, 2, new Nova.Common.DataStructures.NovaPoint(0, 0));
            empire1.Id = Player1Id;
            empire2.Id = Player2Id;
            serverState.AllEmpires[Player1Id].OwnedFleets[fleet1.Key] = fleet1;
            serverState.AllEmpires[Player2Id].OwnedFleets[fleet2.Key] = fleet2;

            // deSerialize
            serverState.Restore();

            // test
            Assert.AreEqual(2101, serverState.TurnYear);            
            Assert.AreEqual("dummy_value", serverState.GameFolder);
            Assert.AreEqual(10, serverState.AllTechLevels[1]);
            Assert.AreEqual(5, serverState.AllTechLevels[2]);
            Assert.AreEqual("foofleet", serverState.AllEmpires[Player1Id].OwnedFleets[fleet1.Key].Name);
            Assert.AreEqual("barfleet", serverState.AllEmpires[Player2Id].OwnedFleets[fleet2.Key].Name);
            Assert.AreEqual(true, serverState.GameInProgress);
        }
    }
}

