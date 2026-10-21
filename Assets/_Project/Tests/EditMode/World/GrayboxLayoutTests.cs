using System;
using System.Collections.Generic;
using CinliMahzen.World.Graybox;
using NUnit.Framework;

namespace CinliMahzen.Tests.EditMode.World
{
    /// <summary>
    /// TODO 1.9: the hand-authored graybox house (<see cref="GrayboxLayout.Default"/>), its validator, the wall planner
    /// and the ASCII map. Pure C#: no Unity objects. Negative tests start from a tiny valid two-floor house
    /// (<see cref="Parts.Mini"/>) and break exactly one rule.
    /// </summary>
    public class GrayboxLayoutTests
    {
        private const int G = GrayboxLayout.GroundFloor;
        private const int U = GrayboxLayout.UpperFloor;

        // ------------------------------------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------------------------------------

        private static GrayboxCell C(int x, int z)
        {
            return new GrayboxCell(x, z);
        }

        private static GrayboxRoom Room(string id, int floor, int x, int z, int width = 1, int depth = 1, bool outdoor = false)
        {
            int stage = outdoor ? 0 : (floor == G ? 1 : 2);
            return new GrayboxRoom(id, id, id.Length > 0 ? id[0] : '?', floor, stage, outdoor,
                outdoor ? GrayboxFloorStyle.Stone : GrayboxFloorStyle.Wood, new GrayboxRect(x, z, width, depth));
        }

        private static GrayboxDoor Door(int floor, int ax, int az, int bx, int bz, GrayboxDoorKind kind)
        {
            return new GrayboxDoor(floor, C(ax, az), C(bx, bz), kind);
        }

        private static GrayboxEdge Edge(int floor, int ax, int az, int bx, int bz)
        {
            return GrayboxEdge.Between(floor, C(ax, az), C(bx, bz));
        }

        private static List<string> Errors(GrayboxLayout layout)
        {
            return GrayboxLayoutValidator.Validate(layout);
        }

        private static bool Has(List<string> errors, string fragment)
        {
            for (int i = 0; i < errors.Count; i++)
            {
                if (errors[i].Contains(fragment))
                {
                    return true;
                }
            }
            return false;
        }

        private static string Dump(List<string> errors)
        {
            return errors.Count == 0 ? "(no errors)" : "validator said:\n  " + string.Join("\n  ", errors);
        }

        private static void AssertRejected(GrayboxLayout layout, string fragment)
        {
            List<string> errors = Errors(layout);
            Assert.IsTrue(Has(errors, fragment), "expected an error containing \"" + fragment + "\"; " + Dump(errors));
            Assert.IsFalse(GrayboxLayoutValidator.IsValid(layout));
        }

        private static int CountAt(List<GrayboxWallSegment> segments, GrayboxEdge edge)
        {
            int count = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].Edge.Equals(edge))
                {
                    count++;
                }
            }
            return count;
        }

        private static GrayboxWallSegment SegmentAt(List<GrayboxWallSegment> segments, GrayboxEdge edge)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].Edge.Equals(edge))
                {
                    return segments[i];
                }
            }
            return null;
        }

        private static int CountKind(List<GrayboxWallSegment> segments, GrayboxWallKind kind)
        {
            int count = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i].Kind == kind)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>Mutable pieces of a layout, so a test can change one thing and build.</summary>
        private sealed class Parts
        {
            public readonly List<GrayboxRoom> Rooms = new List<GrayboxRoom>();
            public readonly List<GrayboxDoor> Doors = new List<GrayboxDoor>();
            public readonly List<GrayboxRailing> Railings = new List<GrayboxRailing>();
            public readonly List<GrayboxStairs> Stairs = new List<GrayboxStairs>();
            public readonly List<GrayboxProp> Props = new List<GrayboxProp>();

            public GrayboxLayout Build()
            {
                return new GrayboxLayout(Rooms, Doors, Railings, Stairs, Props);
            }

            public void RemoveRoom(string id)
            {
                Rooms.RemoveAll(r => r.Id == id);
            }

            public void ReplaceRoom(string id, GrayboxRoom replacement)
            {
                int index = Rooms.FindIndex(r => r.Id == id);
                Assert.GreaterOrEqual(index, 0, "test setup: no room '" + id + "'");
                Rooms[index] = replacement;
            }

            public void RemoveDoor(int floor, int ax, int az, int bx, int bz)
            {
                GrayboxEdge edge = Edge(floor, ax, az, bx, bz);
                int removed = Doors.RemoveAll(d => d.Edge.Equals(edge));
                Assert.AreEqual(1, removed, "test setup: expected one door on " + edge);
            }

            /// <summary>
            /// Smallest valid two-floor house. Graph rules matter here, not stacking, so the upper block sits north of
            /// the ground block.
            ///
            ///   floor 0 (z up)              floor 1
            ///   z=1  [stairwell][nook]      z=3  [up4][up3]
            ///   z=0  [hall     ][den ]      z=2  [up1][up2]     up1 is the stairs arrival
            ///   z=-1 [garden    garden]     x=     0     1
            ///   x=        0       1
            ///
            /// Ground: front door garden-hall; ring hall-den-nook-stairwell-hall (the last link is the stage gate).
            /// Upper: ring up1-up2-up3-up4. Stairs start in the stairwell and climb north.
            /// </summary>
            public static Parts Mini()
            {
                var p = new Parts();
                p.Rooms.Add(Room("garden", G, 0, -1, 2, 1, true));
                p.Rooms.Add(Room("hall", G, 0, 0));
                p.Rooms.Add(Room("den", G, 1, 0));
                p.Rooms.Add(Room("nook", G, 1, 1));
                p.Rooms.Add(Room("stairwell", G, 0, 1));
                p.Rooms.Add(Room("up1", U, 0, 2));
                p.Rooms.Add(Room("up2", U, 1, 2));
                p.Rooms.Add(Room("up3", U, 1, 3));
                p.Rooms.Add(Room("up4", U, 0, 3));

                p.Doors.Add(Door(G, 0, -1, 0, 0, GrayboxDoorKind.FrontDoor));
                p.Doors.Add(Door(G, 0, 0, 1, 0, GrayboxDoorKind.Open));
                p.Doors.Add(Door(G, 1, 0, 1, 1, GrayboxDoorKind.Door));
                p.Doors.Add(Door(G, 1, 1, 0, 1, GrayboxDoorKind.Door));
                p.Doors.Add(Door(G, 0, 0, 0, 1, GrayboxDoorKind.StageGate));
                p.Doors.Add(Door(U, 0, 2, 1, 2, GrayboxDoorKind.Door));
                p.Doors.Add(Door(U, 1, 2, 1, 3, GrayboxDoorKind.Door));
                p.Doors.Add(Door(U, 1, 3, 0, 3, GrayboxDoorKind.Door));
                p.Doors.Add(Door(U, 0, 3, 0, 2, GrayboxDoorKind.Door));

                p.Stairs.Add(new GrayboxStairs(C(0, 1), GrayboxDirection.North));

                p.Props.Add(new GrayboxProp(GrayboxPropKind.Well, 0.5f, -0.5f, 0f));
                p.Props.Add(new GrayboxProp(GrayboxPropKind.Tree, 5.5f, 5.5f, 0f));
                return p;
            }

            /// <summary>Mini plus an upper room "up5" east of the shaft (cell 1,1), reachable from up2.</summary>
            public static Parts MiniWithShaftNeighbor()
            {
                Parts p = Mini();
                p.Rooms.Add(Room("up5", U, 1, 1));
                p.Doors.Add(Door(U, 1, 2, 1, 1, GrayboxDoorKind.Door));
                return p;
            }
        }

        // ------------------------------------------------------------------------------------------------
        // the shipped house
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void Default_PassesValidator()
        {
            List<string> errors = Errors(GrayboxLayout.Default);

            Assert.IsEmpty(errors, Dump(errors));
            Assert.IsTrue(GrayboxLayoutValidator.IsValid(GrayboxLayout.Default));
        }

        [Test]
        public void Default_IsOneSharedInstance()
        {
            Assert.AreSame(GrayboxLayout.Default, GrayboxLayout.Default);
        }

        [Test]
        public void Default_HasGardenShedAndAllStages()
        {
            GrayboxLayout layout = GrayboxLayout.Default;

            Assert.IsNotNull(layout.FindRoom("garden"));
            Assert.IsTrue(layout.FindRoom("garden").IsOutdoor);
            Assert.IsNotNull(layout.FindRoom("shed"));
            Assert.IsFalse(layout.FindRoom("shed").IsOutdoor);

            var stages = new HashSet<int>();
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                stages.Add(layout.Rooms[i].Stage);
            }
            CollectionAssert.IsSupersetOf(stages, new[] { 0, 1, 2 });
        }

        [Test]
        public void Default_HasTheGddRooms()
        {
            // GDD 9: hall, kitchen, living room, pantry, stairs on the ground floor; bedrooms, bathroom, corridor, study upstairs.
            string[] ground = { "hall", "kitchen", "living", "pantry", "stairwell" };
            string[] upper = { "landing", "bedroom1", "bedroom2", "corridor", "bathroom", "study" };

            for (int i = 0; i < ground.Length; i++)
            {
                Assert.IsNotNull(GrayboxLayout.Default.FindRoom(ground[i]), ground[i]);
                Assert.AreEqual(G, GrayboxLayout.Default.FindRoom(ground[i]).Floor, ground[i]);
            }
            for (int i = 0; i < upper.Length; i++)
            {
                Assert.IsNotNull(GrayboxLayout.Default.FindRoom(upper[i]), upper[i]);
                Assert.AreEqual(U, GrayboxLayout.Default.FindRoom(upper[i]).Floor, upper[i]);
            }
        }

        [Test]
        public void Default_RoomSymbolsAreUnique()
        {
            var seen = new HashSet<char>();
            for (int i = 0; i < GrayboxLayout.Default.Rooms.Count; i++)
            {
                GrayboxRoom room = GrayboxLayout.Default.Rooms[i];
                Assert.IsTrue(seen.Add(room.Symbol), "symbol '" + room.Symbol + "' of room '" + room.Id + "' is used twice");
            }
        }

        [Test]
        public void Default_HasOneFrontDoorAndOneStageGatePerStaircase()
        {
            GrayboxLayout layout = GrayboxLayout.Default;
            int front = 0;
            int gates = 0;
            for (int i = 0; i < layout.Doors.Count; i++)
            {
                front += layout.Doors[i].Kind == GrayboxDoorKind.FrontDoor ? 1 : 0;
                gates += layout.Doors[i].Kind == GrayboxDoorKind.StageGate ? 1 : 0;
            }

            int hatches = 0;
            int upStairs = 0;
            for (int i = 0; i < layout.Doors.Count; i++)
            {
                hatches += layout.Doors[i].Kind == GrayboxDoorKind.CellarHatch ? 1 : 0;
            }
            for (int i = 0; i < layout.Stairs.Count; i++)
            {
                upStairs += layout.Stairs[i].IsCellarStairs ? 0 : 1;
            }

            Assert.AreEqual(1, front);
            Assert.AreEqual(upStairs, gates, "one stage gate per staircase going up");
            Assert.AreEqual(1, hatches, "the cellar stairs end at the one cellar hatch");
        }

        [Test]
        public void Default_StairsJoinTheStairwellToTheLanding()
        {
            GrayboxLayout layout = GrayboxLayout.Default;

            Assert.AreEqual(3, layout.Stairs.Count, "main stairs, attic stairs, cellar stairs");
            GrayboxStairs stairs = layout.Stairs[0];
            Assert.AreEqual(G, stairs.BaseFloor);
            Assert.AreEqual("stairwell", layout.RoomAt(G, stairs.Start).Id);
            Assert.AreEqual("hall", layout.RoomAt(G, stairs.Entrance).Id);
            Assert.AreEqual("landing", layout.RoomAt(U, stairs.Arrival).Id);
            Assert.IsTrue(layout.IsStairVoid(U, stairs.Start), "the shaft must stay open on the upper floor");
            Assert.IsFalse(layout.IsStairVoid(G, stairs.Start));
            Assert.AreEqual(GrayboxDoorKind.StageGate, layout.FindDoor(stairs.EntranceEdge).Kind);
        }

        [Test]
        public void Default_AtticStairsRiseFromTheCorridor_BehindTheStage2Gate()
        {
            GrayboxLayout layout = GrayboxLayout.Default;

            GrayboxStairs stairs = layout.Stairs[1];
            Assert.AreEqual(U, stairs.BaseFloor);
            Assert.AreEqual("atticstairs", layout.RoomAt(U, stairs.Start).Id);
            Assert.AreEqual("corridor", layout.RoomAt(U, stairs.Entrance).Id);
            Assert.AreEqual("attic", layout.RoomAt(GrayboxLayout.AtticFloor, stairs.Arrival).Id);
            Assert.IsTrue(layout.IsStairVoid(GrayboxLayout.AtticFloor, stairs.Start), "the attic floor stays open above the stairs");
            GrayboxDoor gate = layout.FindDoor(stairs.EntranceEdge);
            Assert.AreEqual(GrayboxDoorKind.StageGate, gate.Kind);
            Assert.AreEqual(2, layout.RoomAt(U, stairs.Start).Stage);
        }

        [Test]
        public void Default_CellarStairsClimbToTheHatchInThePantry()
        {
            GrayboxLayout layout = GrayboxLayout.Default;

            GrayboxStairs stairs = layout.Stairs[2];
            Assert.IsTrue(stairs.IsCellarStairs);
            Assert.AreEqual("cellar", layout.RoomAt(GrayboxLayout.CellarFloor, stairs.Start).Id);
            Assert.AreEqual("cellar", layout.RoomAt(GrayboxLayout.CellarFloor, stairs.Entrance).Id, "the bottom opens into the cellar");
            Assert.AreEqual("pantry", layout.RoomAt(G, stairs.Arrival).Id);
            Assert.IsTrue(layout.IsStairVoid(G, stairs.Start), "the hatch hole in the ground floor");
            Assert.IsNull(layout.RoomAt(G, stairs.Start));
            Assert.AreEqual(GrayboxDoorKind.CellarHatch, layout.FindDoor(stairs.ArrivalEdge).Kind);
        }

        [Test]
        public void Default_HasAllFourLevels()
        {
            GrayboxLayout layout = GrayboxLayout.Default;
            var floors = new HashSet<int>();
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                floors.Add(layout.Rooms[i].Floor);
            }

            CollectionAssert.AreEquivalent(new[] { GrayboxLayout.CellarFloor, G, U, GrayboxLayout.AtticFloor }, floors);
            Assert.AreEqual(3, layout.FindRoom("attic").Stage);
            Assert.AreEqual(3, layout.FindRoom("ritual").Stage);
            Assert.IsFalse(layout.FindRoom("cellar").IsOutdoor);
        }

        [Test]
        public void Default_RailingsOnlyLineTheShaft()
        {
            GrayboxLayout layout = GrayboxLayout.Default;

            Assert.GreaterOrEqual(layout.Railings.Count, 1);
            for (int i = 0; i < layout.Railings.Count; i++)
            {
                GrayboxRailing railing = layout.Railings[i];
                bool voidA = layout.IsStairVoid(railing.Floor, railing.CellA);
                bool voidB = layout.IsStairVoid(railing.Floor, railing.CellB);
                Assert.AreNotEqual(voidA, voidB, railing.ToString());
            }
        }

        [Test]
        public void Default_CellBoundsCoverGardenAndHouse()
        {
            int minX, minZ, maxX, maxZ;
            GrayboxLayout.Default.GetCellBounds(out minX, out minZ, out maxX, out maxZ);

            Assert.LessOrEqual(minX, 0);
            Assert.LessOrEqual(minZ, -1, "the garden lies in front of the house (south, -Z)");
            Assert.GreaterOrEqual(maxX, 4);
            Assert.GreaterOrEqual(maxZ, 3);
            Assert.IsNotNull(GrayboxLayout.Default.RoomAt(G, C(2, 0)), "the hall");
            Assert.IsNull(GrayboxLayout.Default.RoomAt(G, C(minX - 1, minZ - 1)));
        }

        // ------------------------------------------------------------------------------------------------
        // validator: the mini house is valid, every rule has a negative test
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void Mini_IsValid()
        {
            List<string> errors = Errors(Parts.Mini().Build());

            Assert.IsEmpty(errors, Dump(errors));
        }

        [Test]
        public void Mini_WithShaftNeighborAndRailing_IsValid()
        {
            Parts p = Parts.MiniWithShaftNeighbor();
            p.Railings.Add(new GrayboxRailing(U, C(0, 1), C(1, 1)));

            List<string> errors = Errors(p.Build());

            Assert.IsEmpty(errors, Dump(errors));
        }

        [Test]
        public void Rejects_NullLayout()
        {
            Assert.IsTrue(Has(GrayboxLayoutValidator.Validate(null), "layout is null"));
        }

        [Test]
        public void Rejects_LayoutWithoutRooms()
        {
            var empty = new GrayboxLayout(new GrayboxRoom[0], new GrayboxDoor[0], new GrayboxRailing[0],
                new GrayboxStairs[0], new GrayboxProp[0]);

            AssertRejected(empty, "layout has no rooms");
        }

        [Test]
        public void Rejects_EmptyRoomId()
        {
            Parts p = Parts.Mini();
            p.ReplaceRoom("den", Room("", G, 1, 0));

            AssertRejected(p.Build(), "has an empty id");
        }

        [Test]
        public void Rejects_DuplicateRoomId()
        {
            Parts p = Parts.Mini();
            p.Rooms.Add(Room("hall", G, 3, 0));

            AssertRejected(p.Build(), "duplicate room id 'hall'");
        }

        [Test]
        public void Rejects_RoomOnAFloorThatDoesNotExist()
        {
            Parts p = Parts.Mini();
            p.Rooms.Add(Room("tower", 3, 0, 0));

            AssertRejected(p.Build(), "room 'tower' has floor 3");
        }

        [Test]
        public void Rejects_OutdoorRoomOnTheUpperFloor()
        {
            Parts p = Parts.Mini();
            p.Rooms.Add(Room("balcony", U, 5, 5, 1, 1, true));

            AssertRejected(p.Build(), "outdoor room 'balcony' must be on the ground floor");
        }

        [Test]
        public void Rejects_RoomWithoutRectangles()
        {
            Parts p = Parts.Mini();
            p.Rooms.Add(new GrayboxRoom("ghost", "Ghost", 'g', G, 1, false, GrayboxFloorStyle.Wood));

            AssertRejected(p.Build(), "room 'ghost' has no cell rectangle");
        }

        [Test]
        public void Rejects_EmptyRectangle()
        {
            Parts p = Parts.Mini();
            p.Rooms.Add(new GrayboxRoom("flat", "Flat", 'f', G, 1, false, GrayboxFloorStyle.Wood, new GrayboxRect(5, 5, 0, 1)));

            AssertRejected(p.Build(), "room 'flat' has an empty rectangle");
        }

        [Test]
        public void Rejects_OverlappingRooms()
        {
            Parts p = Parts.Mini();
            p.Rooms.Add(Room("closet", G, 1, 0)); // the den already owns (1,0)

            AssertRejected(p.Build(), "rooms 'den' and 'closet' overlap at cell (1,0)");
        }

        [Test]
        public void Rejects_RoomThatCoversACellTwice()
        {
            Parts p = Parts.Mini();
            p.Rooms.Add(new GrayboxRoom("twice", "Twice", 't', G, 1, false, GrayboxFloorStyle.Wood,
                new GrayboxRect(4, 4, 2, 1), new GrayboxRect(5, 4, 1, 1)));

            AssertRejected(p.Build(), "room 'twice' covers cell (5,4) twice");
        }

        [Test]
        public void Accepts_RoomsOnDifferentFloorsSharingACell()
        {
            Parts p = Parts.Mini();
            p.Rooms.Add(Room("above_hall", U, 0, 0)); // stacked over the hall

            Assert.IsFalse(Has(Errors(p.Build()), "overlap"));
        }

        [Test]
        public void Rejects_DoorOnAFloorThatDoesNotExist()
        {
            Parts p = Parts.Mini();
            p.Doors.Add(Door(5, 0, 0, 1, 0, GrayboxDoorKind.Door));

            AssertRejected(p.Build(), "floor 5 does not exist");
        }

        [Test]
        public void Rejects_TwoDoorsOnTheSameBorder()
        {
            Parts p = Parts.Mini();
            p.Doors.Add(Door(G, 1, 0, 0, 0, GrayboxDoorKind.Door)); // hall-den already has an Open door

            AssertRejected(p.Build(), "two doors on the same border");
        }

        [Test]
        public void Rejects_DoorThatLeadsNowhere()
        {
            Parts p = Parts.Mini();
            p.Doors.Add(Door(G, 1, 1, 2, 1, GrayboxDoorKind.Door)); // (2,1) has no room

            AssertRejected(p.Build(), "does not connect two rooms");
        }

        [Test]
        public void Rejects_DoorInsideOneRoom()
        {
            Parts p = Parts.Mini();
            p.Rooms.Add(Room("wide", G, 3, 0, 2, 1));
            p.Doors.Add(Door(G, 3, 0, 4, 0, GrayboxDoorKind.Door));

            AssertRejected(p.Build(), "both cells belong to room 'wide'");
        }

        [Test]
        public void Rejects_MissingFrontDoor()
        {
            Parts p = Parts.Mini();
            p.RemoveDoor(G, 0, -1, 0, 0);

            AssertRejected(p.Build(), "expected exactly one FrontDoor, found 0");
        }

        [Test]
        public void Rejects_TwoFrontDoors()
        {
            Parts p = Parts.Mini();
            p.Doors.Add(Door(G, 1, -1, 1, 0, GrayboxDoorKind.FrontDoor));

            AssertRejected(p.Build(), "expected exactly one FrontDoor, found 2");
        }

        [Test]
        public void Rejects_FrontDoorBetweenTwoIndoorRooms()
        {
            Parts p = Parts.Mini();
            p.RemoveDoor(G, 0, -1, 0, 0);
            p.RemoveDoor(G, 0, 0, 1, 0);
            p.Doors.Add(Door(G, 0, 0, 1, 0, GrayboxDoorKind.FrontDoor)); // hall-den, both indoor

            AssertRejected(p.Build(), "the FrontDoor must connect an outdoor room and an indoor room");
        }

        [Test]
        public void Rejects_FrontDoorOnTheUpperFloor()
        {
            Parts p = Parts.Mini();
            p.RemoveDoor(G, 0, -1, 0, 0);
            p.RemoveDoor(U, 0, 2, 1, 2);
            p.Doors.Add(Door(U, 0, 2, 1, 2, GrayboxDoorKind.FrontDoor));

            AssertRejected(p.Build(), "the FrontDoor must connect an outdoor room and an indoor room");
        }

        [Test]
        public void Rejects_UpperRoomsWithoutStairs()
        {
            Parts p = Parts.Mini();
            p.Stairs.Clear();

            AssertRejected(p.Build(), "rooms on floor 1 but no staircase");
        }

        [Test]
        public void Rejects_StairsOutsideEveryRoom()
        {
            Parts p = Parts.Mini();
            p.Stairs[0] = new GrayboxStairs(C(7, 7), GrayboxDirection.North);

            AssertRejected(p.Build(), "is not inside a room of floor 0");
        }

        [Test]
        public void Rejects_StairsInTheGarden()
        {
            Parts p = Parts.Mini();
            p.Stairs[0] = new GrayboxStairs(C(1, -1), GrayboxDirection.North);

            AssertRejected(p.Build(), "the stairs stand in outdoor room 'garden'");
        }

        [Test]
        public void Rejects_ShaftThatIsCoveredByAnUpperRoom()
        {
            Parts p = Parts.Mini();
            p.Rooms.Add(Room("cover", U, 0, 1)); // right above the stairwell

            AssertRejected(p.Build(), "is covered by floor 1 room 'cover'");
        }

        [Test]
        public void Rejects_StairsThatArriveInNoRoom()
        {
            Parts p = Parts.Mini();
            p.Stairs[0] = new GrayboxStairs(C(1, 1), GrayboxDirection.East); // arrival (2,1) is empty upstairs

            AssertRejected(p.Build(), "arrival cell (2,1) is not inside a room of floor 1");
        }

        [Test]
        public void Rejects_StairsWhoseEntranceIsNoRoom()
        {
            Parts p = Parts.Mini();
            p.Stairs[0] = new GrayboxStairs(C(0, 1), GrayboxDirection.South); // entrance (0,2) has no ground room

            AssertRejected(p.Build(), "entrance cell (0,2) is not inside a room of floor 0");
        }

        // ---- cellar (TODO 4.1) ----

        /// <summary>
        /// Mini plus a one-room cellar east of the house: cellar cells (2,0)-(3,0) on floor -1, stairs from (2,0) climbing west to
        /// the den (1,0), the cellar hatch on the den's east border. Ground cell (2,0) is the hatch hole.
        /// </summary>
        private static Parts MiniWithCellar()
        {
            Parts p = Parts.Mini();
            p.Rooms.Add(new GrayboxRoom("cellar", "Cellar", 'Z', GrayboxLayout.CellarFloor, 0, false, GrayboxFloorStyle.Stone,
                new GrayboxRect(2, 0, 2, 1)));
            p.Stairs.Add(new GrayboxStairs(C(2, 0), GrayboxDirection.West, GrayboxLayout.CellarFloor));
            p.Doors.Add(Door(G, 1, 0, 2, 0, GrayboxDoorKind.CellarHatch));
            return p;
        }

        [Test]
        public void Accepts_ACellarOfOneRoom_ReachedThroughTheHatch()
        {
            GrayboxLayout layout = MiniWithCellar().Build();

            List<string> errors = Errors(layout);
            Assert.IsEmpty(errors, Dump(errors));
            Assert.IsTrue(layout.IsStairVoid(G, C(2, 0)));
            Assert.IsFalse(layout.IsStairVoid(GrayboxLayout.CellarFloor, C(2, 0)));
        }

        [Test]
        public void Rejects_CellarStairsWithoutAHatch()
        {
            Parts p = MiniWithCellar();
            p.RemoveDoor(G, 1, 0, 2, 0);

            AssertRejected(p.Build(), "needs a CellarHatch door");
        }

        [Test]
        public void Rejects_AHatchThatIsNotOnTheCellarStairs()
        {
            Parts p = MiniWithCellar();
            p.RemoveDoor(G, 0, 0, 1, 0);
            p.Doors.Add(Door(G, 0, 0, 1, 0, GrayboxDoorKind.CellarHatch));

            AssertRejected(p.Build(), "a CellarHatch must sit on the arrival border of the cellar stairs");
        }

        [Test]
        public void Rejects_ACellarWithoutStairs()
        {
            Parts p = MiniWithCellar();
            p.Stairs.RemoveAt(p.Stairs.Count - 1);

            AssertRejected(p.Build(), "rooms on floor -1 but no staircase");
        }

        [Test]
        public void Planner_BuildsTheHatchDoor_AndWallsAroundTheHole()
        {
            GrayboxLayout layout = MiniWithCellar().Build();

            List<GrayboxWallSegment> segments = GrayboxWallPlanner.PlanWalls(layout);

            GrayboxWallSegment hatch = SegmentAt(segments, Edge(G, 1, 0, 2, 0));
            Assert.IsNotNull(hatch);
            Assert.AreEqual(GrayboxWallKind.Door, hatch.Kind);
            Assert.AreEqual(GrayboxDoorKind.CellarHatch, hatch.Door.Kind);
            Assert.AreEqual("den", hatch.OwnerRoomId);
            Assert.AreEqual(GrayboxWallKind.Wall, SegmentAt(segments, Edge(GrayboxLayout.CellarFloor, 3, 0, 4, 0)).Kind, "cellar outer wall");
        }

        [Test]
        public void Rejects_StairsWhoseEntranceIsInTheStairwellItself()
        {
            Parts p = Parts.Mini();
            p.RemoveRoom("hall");
            p.ReplaceRoom("stairwell", Room("stairwell", G, 0, 0, 1, 2)); // (0,0) and (0,1)

            AssertRejected(p.Build(), "entrance cell (0,0) is in the stairwell room itself");
        }

        [Test]
        public void Rejects_StairsWithoutAStageGate()
        {
            Parts p = Parts.Mini();
            p.RemoveDoor(G, 0, 0, 0, 1);
            p.Doors.Add(Door(G, 0, 0, 0, 1, GrayboxDoorKind.Door)); // plain door instead of the gate

            AssertRejected(p.Build(), "needs a StageGate door");
        }

        [Test]
        public void Rejects_StairsWithNoDoorAtTheEntrance()
        {
            Parts p = Parts.Mini();
            p.RemoveDoor(G, 0, 0, 0, 1);

            AssertRejected(p.Build(), "needs a StageGate door");
        }

        [Test]
        public void Rejects_RailingOnTheGroundFloor()
        {
            Parts p = Parts.Mini();
            p.Railings.Add(new GrayboxRailing(G, C(0, 0), C(1, 0)));

            AssertRejected(p.Build(), "railings belong on the upper floor");
        }

        [Test]
        public void Rejects_RailingAwayFromTheShaft()
        {
            Parts p = Parts.Mini();
            p.Railings.Add(new GrayboxRailing(U, C(0, 2), C(1, 2))); // up1-up2, no shaft involved

            AssertRejected(p.Build(), "must lie between the stair shaft and a room cell");
        }

        [Test]
        public void Rejects_RailingTowardsAnEmptyCell()
        {
            Parts p = Parts.Mini();
            p.Railings.Add(new GrayboxRailing(U, C(0, 1), C(1, 1))); // (1,1) has no upper room in the plain mini

            AssertRejected(p.Build(), "cell (1,1) has no room");
        }

        [Test]
        public void Rejects_RailingThatBlocksTheArrival()
        {
            Parts p = Parts.Mini();
            p.Railings.Add(new GrayboxRailing(U, C(0, 1), C(0, 2))); // the arrival border

            AssertRejected(p.Build(), "blocks the stairs arrival border");
        }

        [Test]
        public void Rejects_RoomNobodyCanReach()
        {
            Parts p = Parts.Mini();
            p.Rooms.Add(Room("closet", G, 3, 3)); // no door at all

            List<string> errors = Errors(p.Build());

            Assert.IsTrue(Has(errors, "room 'closet' is unreachable from the front door"), Dump(errors));
            Assert.IsFalse(Has(errors, "room 'hall' is unreachable"), Dump(errors));
        }

        [Test]
        public void Rejects_UpperRoomWithNeitherDoorNorStairs()
        {
            Parts p = Parts.Mini();
            p.Rooms.Add(Room("island", U, 5, 5));

            List<string> errors = Errors(p.Build());

            Assert.IsTrue(Has(errors, "room 'island' is unreachable from the front door"), Dump(errors));
            Assert.IsFalse(Has(errors, "room 'up1' is unreachable"), "the stairs do reach the upper block; " + Dump(errors));
        }

        [Test]
        public void Rejects_GroundFloorWithoutALoop()
        {
            Parts p = Parts.Mini();
            p.RemoveDoor(G, 1, 0, 1, 1); // opens the ring hall-den-nook-stairwell

            List<string> errors = Errors(p.Build());

            Assert.IsTrue(Has(errors, "floor 0 has no loop"), Dump(errors));
            Assert.IsFalse(Has(errors, "floor 1 has no loop"), Dump(errors));
            Assert.IsFalse(Has(errors, "unreachable"), "everything is still connected, only the loop is gone; " + Dump(errors));
        }

        [Test]
        public void Rejects_UpperFloorWithoutALoop()
        {
            Parts p = Parts.Mini();
            p.RemoveDoor(U, 0, 3, 0, 2);

            List<string> errors = Errors(p.Build());

            Assert.IsTrue(Has(errors, "floor 1 has no loop"), Dump(errors));
            Assert.IsFalse(Has(errors, "floor 0 has no loop"), Dump(errors));
        }

        [Test]
        public void Rejects_TreeInsideARoom()
        {
            Parts p = Parts.Mini();
            p.Props.Add(new GrayboxProp(GrayboxPropKind.Tree, 0.5f, 0.5f, 0f)); // inside the hall

            AssertRejected(p.Build(), "trees stand outside every room (found in 'hall')");
        }

        [TestCase(GrayboxPropKind.Well)]
        [TestCase(GrayboxPropKind.Woodpile)]
        [TestCase(GrayboxPropKind.Pot)]
        public void Rejects_GardenPropOutsideTheGarden(GrayboxPropKind kind)
        {
            Parts p = Parts.Mini();
            p.Props.Add(new GrayboxProp(kind, 6.5f, 6.5f, 0f)); // no room at all
            p.Props.Add(new GrayboxProp(kind, 0.5f, 0.5f, 0f)); // inside the hall

            List<string> errors = Errors(p.Build());
            int hits = 0;
            for (int i = 0; i < errors.Count; i++)
            {
                hits += errors[i].Contains("must stand inside the garden") ? 1 : 0;
            }

            Assert.AreEqual(2, hits, Dump(errors));
        }

        // ------------------------------------------------------------------------------------------------
        // wall planner
        // ------------------------------------------------------------------------------------------------

        private static Parts TwoRooms(bool withDoor, GrayboxDoorKind doorKind = GrayboxDoorKind.Door)
        {
            var p = new Parts();
            p.Rooms.Add(Room("a", G, 0, 0));
            p.Rooms.Add(Room("b", G, 1, 0));
            if (withDoor)
            {
                p.Doors.Add(Door(G, 0, 0, 1, 0, doorKind));
            }
            return p;
        }

        [Test]
        public void Planner_SharedWallIsBuiltOnce()
        {
            GrayboxLayout layout = TwoRooms(false).Build();

            List<GrayboxWallSegment> segments = GrayboxWallPlanner.PlanWalls(layout);

            GrayboxEdge shared = Edge(G, 0, 0, 1, 0);
            Assert.AreEqual(1, CountAt(segments, shared));
            Assert.AreEqual(GrayboxWallKind.Wall, SegmentAt(segments, shared).Kind);
            Assert.IsNull(SegmentAt(segments, shared).Door);
            Assert.AreEqual(7, segments.Count, "3 outer sides per room + 1 shared");
            Assert.AreEqual(7, CountKind(segments, GrayboxWallKind.Wall));
        }

        [Test]
        public void Planner_SharedWallIsTheSameWhateverTheRoomOrder()
        {
            var p = new Parts();
            p.Rooms.Add(Room("b", G, 1, 0));
            p.Rooms.Add(Room("a", G, 0, 0));

            List<GrayboxWallSegment> segments = GrayboxWallPlanner.PlanWalls(p.Build());

            Assert.AreEqual(1, CountAt(segments, Edge(G, 0, 0, 1, 0)));
            Assert.AreEqual(7, segments.Count);
        }

        [TestCase(GrayboxDoorKind.Open)]
        [TestCase(GrayboxDoorKind.Door)]
        [TestCase(GrayboxDoorKind.StageGate)]
        [TestCase(GrayboxDoorKind.FrontDoor)]
        public void Planner_DoorEdgeBecomesOneDoorSegmentWithItsKind(GrayboxDoorKind kind)
        {
            Parts p = TwoRooms(true, kind);

            List<GrayboxWallSegment> segments = GrayboxWallPlanner.PlanWalls(p.Build());

            GrayboxEdge shared = Edge(G, 0, 0, 1, 0);
            Assert.AreEqual(1, CountAt(segments, shared));
            GrayboxWallSegment segment = SegmentAt(segments, shared);
            Assert.AreEqual(GrayboxWallKind.Door, segment.Kind);
            Assert.AreSame(p.Doors[0], segment.Door);
            Assert.AreEqual(kind, segment.Door.Kind);
            Assert.AreEqual(1, CountKind(segments, GrayboxWallKind.Door));
            Assert.AreEqual(6, CountKind(segments, GrayboxWallKind.Wall));
        }

        [Test]
        public void Planner_SharedWallBelongsToTheIndoorRoom()
        {
            foreach (bool gardenFirst in new[] { true, false })
            {
                var p = new Parts();
                if (gardenFirst)
                {
                    p.Rooms.Add(Room("yard", G, 0, 0, 1, 1, true));
                    p.Rooms.Add(Room("house", G, 1, 0));
                }
                else
                {
                    p.Rooms.Add(Room("house", G, 1, 0));
                    p.Rooms.Add(Room("yard", G, 0, 0, 1, 1, true));
                }

                List<GrayboxWallSegment> segments = GrayboxWallPlanner.PlanWalls(p.Build());

                GrayboxWallSegment shared = SegmentAt(segments, Edge(G, 0, 0, 1, 0));
                Assert.AreEqual("house", shared.OwnerRoomId, "garden listed first: " + gardenFirst);
                Assert.AreEqual(GrayboxWallKind.Wall, shared.Kind);
            }
        }

        [Test]
        public void Planner_BothCellsInOneRoomNeedNoWall()
        {
            var p = new Parts();
            p.Rooms.Add(Room("long", G, 0, 0, 2, 1));

            List<GrayboxWallSegment> segments = GrayboxWallPlanner.PlanWalls(p.Build());

            Assert.AreEqual(0, CountAt(segments, Edge(G, 0, 0, 1, 0)));
            Assert.AreEqual(6, segments.Count);
        }

        [Test]
        public void Planner_OutdoorBorderIsAFenceAndIndoorBorderIsAWall()
        {
            var p = new Parts();
            p.Rooms.Add(Room("yard", G, 0, 0, 1, 1, true));
            p.Rooms.Add(Room("room", G, 5, 5));

            List<GrayboxWallSegment> segments = GrayboxWallPlanner.PlanWalls(p.Build());

            Assert.AreEqual(8, segments.Count);
            for (int i = 0; i < segments.Count; i++)
            {
                GrayboxWallKind expected = segments[i].OwnerRoomId == "yard" ? GrayboxWallKind.Fence : GrayboxWallKind.Wall;
                Assert.AreEqual(expected, segments[i].Kind, segments[i].ToString());
            }
            Assert.AreEqual(4, CountKind(segments, GrayboxWallKind.Fence));
            Assert.AreEqual(4, CountKind(segments, GrayboxWallKind.Wall));
        }

        [Test]
        public void Planner_FloorsAreSeparate()
        {
            var p = new Parts();
            p.Rooms.Add(Room("low", G, 0, 0));
            p.Rooms.Add(Room("high", U, 0, 0));

            List<GrayboxWallSegment> segments = GrayboxWallPlanner.PlanWalls(p.Build());

            Assert.AreEqual(8, segments.Count, "4 walls per floor, nothing is shared across floors");
            Assert.AreEqual(1, CountAt(segments, Edge(G, 0, 0, 1, 0)));
            Assert.AreEqual(1, CountAt(segments, Edge(U, 0, 0, 1, 0)));
        }

        [Test]
        public void Planner_ShaftBorderIsAWallUnlessARailingIsDeclared()
        {
            GrayboxEdge towardsNeighbor = Edge(U, 0, 1, 1, 1);

            List<GrayboxWallSegment> plain = GrayboxWallPlanner.PlanWalls(Parts.MiniWithShaftNeighbor().Build());
            Parts withRailing = Parts.MiniWithShaftNeighbor();
            withRailing.Railings.Add(new GrayboxRailing(U, C(0, 1), C(1, 1)));
            List<GrayboxWallSegment> railed = GrayboxWallPlanner.PlanWalls(withRailing.Build());

            Assert.AreEqual(GrayboxWallKind.Wall, SegmentAt(plain, towardsNeighbor).Kind);
            Assert.AreEqual(GrayboxWallKind.Railing, SegmentAt(railed, towardsNeighbor).Kind);
            Assert.AreEqual("up5", SegmentAt(railed, towardsNeighbor).OwnerRoomId);
            Assert.AreEqual(1, CountAt(railed, towardsNeighbor));
        }

        [Test]
        public void Planner_StairsArrivalBorderStaysOpen()
        {
            GrayboxLayout layout = Parts.Mini().Build();

            List<GrayboxWallSegment> segments = GrayboxWallPlanner.PlanWalls(layout);

            Assert.AreEqual(0, CountAt(segments, layout.Stairs[0].ArrivalEdge));
        }

        [Test]
        public void Planner_Default_EveryEdgeAppearsOnce()
        {
            List<GrayboxWallSegment> segments = GrayboxWallPlanner.PlanWalls(GrayboxLayout.Default);

            var seen = new HashSet<GrayboxEdge>();
            for (int i = 0; i < segments.Count; i++)
            {
                Assert.IsTrue(seen.Add(segments[i].Edge), "built twice: " + segments[i]);
            }
            Assert.Greater(segments.Count, 0);
        }

        [Test]
        public void Planner_Default_EveryDoorBecomesExactlyOneDoorSegment()
        {
            GrayboxLayout layout = GrayboxLayout.Default;

            List<GrayboxWallSegment> segments = GrayboxWallPlanner.PlanWalls(layout);

            Assert.AreEqual(layout.Doors.Count, CountKind(segments, GrayboxWallKind.Door));
            for (int i = 0; i < layout.Doors.Count; i++)
            {
                GrayboxWallSegment segment = SegmentAt(segments, layout.Doors[i].Edge);
                Assert.IsNotNull(segment, layout.Doors[i].ToString());
                Assert.AreEqual(GrayboxWallKind.Door, segment.Kind, layout.Doors[i].ToString());
                Assert.AreSame(layout.Doors[i], segment.Door, layout.Doors[i].ToString());
            }
        }

        [Test]
        public void Planner_Default_RailingsAndFencesAreWhereTheyBelong()
        {
            GrayboxLayout layout = GrayboxLayout.Default;

            List<GrayboxWallSegment> segments = GrayboxWallPlanner.PlanWalls(layout);

            Assert.AreEqual(layout.Railings.Count, CountKind(segments, GrayboxWallKind.Railing));
            for (int i = 0; i < segments.Count; i++)
            {
                GrayboxWallSegment segment = segments[i];
                if (segment.Kind == GrayboxWallKind.Fence)
                {
                    Assert.IsTrue(layout.FindRoom(segment.OwnerRoomId).IsOutdoor, segment.ToString());
                }
                if (segment.Kind == GrayboxWallKind.Railing)
                {
                    Assert.GreaterOrEqual(segment.Edge.Floor, U, segment.ToString());
                    Assert.IsNotNull(layout.FindRailing(segment.Edge), segment.ToString());
                }
            }
        }

        [Test]
        public void Planner_Default_NothingStandsInsideARoomOrOnTheArrivalBorder()
        {
            GrayboxLayout layout = GrayboxLayout.Default;

            List<GrayboxWallSegment> segments = GrayboxWallPlanner.PlanWalls(layout);

            for (int i = 0; i < segments.Count; i++)
            {
                GrayboxEdge edge = segments[i].Edge;
                int low = layout.RoomIndexAt(edge.Floor, edge.LowCell);
                int high = layout.RoomIndexAt(edge.Floor, edge.HighCell);
                Assert.IsFalse(low >= 0 && low == high, "wall inside one room: " + segments[i]);
            }
            for (int s = 0; s < layout.Stairs.Count; s++)
            {
                GrayboxStairs stairs = layout.Stairs[s];
                if (!stairs.IsCellarStairs)
                {
                    Assert.AreEqual(0, CountAt(segments, stairs.ArrivalEdge), stairs.ToString());
                    continue;
                }
                GrayboxWallSegment hatch = SegmentAt(segments, stairs.ArrivalEdge);
                Assert.IsNotNull(hatch, "the cellar hatch closes the top of the cellar stairs");
                Assert.AreEqual(GrayboxWallKind.Door, hatch.Kind);
                Assert.AreEqual(GrayboxDoorKind.CellarHatch, hatch.Door.Kind);
            }
        }

        // ------------------------------------------------------------------------------------------------
        // posts
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void Posts_IndoorRoomGetsFourPillars()
        {
            var p = new Parts();
            p.Rooms.Add(Room("room", G, 0, 0));

            List<GrayboxPost> posts = GrayboxWallPlanner.PlanPosts(GrayboxWallPlanner.PlanWalls(p.Build()));

            Assert.AreEqual(4, posts.Count);
            for (int i = 0; i < posts.Count; i++)
            {
                Assert.AreEqual(GrayboxPostKind.Pillar, posts[i].Kind);
            }
        }

        [Test]
        public void Posts_GardenFenceGetsFencePostsAndAWallTurnsAVertexIntoAPillar()
        {
            var fenceOnly = new Parts();
            fenceOnly.Rooms.Add(Room("yard", G, 0, 0, 1, 1, true));
            List<GrayboxPost> fencePosts = GrayboxWallPlanner.PlanPosts(GrayboxWallPlanner.PlanWalls(fenceOnly.Build()));

            Assert.AreEqual(4, fencePosts.Count);
            for (int i = 0; i < fencePosts.Count; i++)
            {
                Assert.AreEqual(GrayboxPostKind.FencePost, fencePosts[i].Kind);
            }

            // A house cell right next to the yard: the two shared vertices (1,0) and (1,1) now carry a wall.
            var mixed = new Parts();
            mixed.Rooms.Add(Room("yard", G, 0, 0, 1, 1, true));
            mixed.Rooms.Add(Room("house", G, 1, 0));
            List<GrayboxPost> mixedPosts = GrayboxWallPlanner.PlanPosts(GrayboxWallPlanner.PlanWalls(mixed.Build()));

            Assert.AreEqual(6, mixedPosts.Count, "2x1 cells have 6 vertices");
            Assert.AreEqual(GrayboxPostKind.Pillar, PostAt(mixedPosts, G, 1, 0).Kind);
            Assert.AreEqual(GrayboxPostKind.Pillar, PostAt(mixedPosts, G, 1, 1).Kind);
            Assert.AreEqual(GrayboxPostKind.FencePost, PostAt(mixedPosts, G, 0, 0).Kind);
            Assert.AreEqual(GrayboxPostKind.FencePost, PostAt(mixedPosts, G, 0, 1).Kind);
            Assert.AreEqual(GrayboxPostKind.Pillar, PostAt(mixedPosts, G, 2, 0).Kind);
        }

        private static GrayboxPost PostAt(List<GrayboxPost> posts, int floor, int vx, int vz)
        {
            for (int i = 0; i < posts.Count; i++)
            {
                if (posts[i].Floor == floor && posts[i].VertexX == vx && posts[i].VertexZ == vz)
                {
                    return posts[i];
                }
            }
            Assert.Fail("no post at floor " + floor + " vertex (" + vx + "," + vz + ")");
            return default(GrayboxPost);
        }

        [Test]
        public void Posts_Default_OnePostPerVertexAndAllEndpointsCovered()
        {
            List<GrayboxWallSegment> segments = GrayboxWallPlanner.PlanWalls(GrayboxLayout.Default);

            List<GrayboxPost> posts = GrayboxWallPlanner.PlanPosts(segments);

            var postKeys = new HashSet<string>();
            for (int i = 0; i < posts.Count; i++)
            {
                Assert.IsTrue(postKeys.Add(posts[i].Floor + ":" + posts[i].VertexX + ":" + posts[i].VertexZ), "two posts at " + posts[i]);
            }
            var endpointKeys = new HashSet<string>();
            for (int i = 0; i < segments.Count; i++)
            {
                int ax, az, bx, bz;
                segments[i].Edge.GetVertices(out ax, out az, out bx, out bz);
                endpointKeys.Add(segments[i].Edge.Floor + ":" + ax + ":" + az);
                endpointKeys.Add(segments[i].Edge.Floor + ":" + bx + ":" + bz);
            }
            CollectionAssert.AreEquivalent(endpointKeys, postKeys);
        }

        [Test]
        public void Posts_Default_PillarWhereverASolidPieceEnds()
        {
            List<GrayboxWallSegment> segments = GrayboxWallPlanner.PlanWalls(GrayboxLayout.Default);
            List<GrayboxPost> posts = GrayboxWallPlanner.PlanPosts(segments);

            for (int i = 0; i < segments.Count; i++)
            {
                bool solid = segments[i].Kind == GrayboxWallKind.Wall || segments[i].Kind == GrayboxWallKind.Door;
                if (!solid)
                {
                    continue;
                }
                int ax, az, bx, bz;
                segments[i].Edge.GetVertices(out ax, out az, out bx, out bz);
                Assert.AreEqual(GrayboxPostKind.Pillar, PostAt(posts, segments[i].Edge.Floor, ax, az).Kind, segments[i].ToString());
                Assert.AreEqual(GrayboxPostKind.Pillar, PostAt(posts, segments[i].Edge.Floor, bx, bz).Kind, segments[i].ToString());
            }
        }

        // ------------------------------------------------------------------------------------------------
        // grid primitives
        // ------------------------------------------------------------------------------------------------

        [Test]
        public void Edge_IsTheSameFromBothSides()
        {
            Assert.AreEqual(Edge(G, 2, 3, 3, 3), Edge(G, 3, 3, 2, 3));
            Assert.AreEqual(Edge(U, 2, 3, 2, 4), Edge(U, 2, 4, 2, 3));
            Assert.AreNotEqual(Edge(G, 2, 3, 2, 4), Edge(U, 2, 3, 2, 4));
        }

        [Test]
        public void Edge_RunsAlongTheAxisPerpendicularToTheNeighbour()
        {
            GrayboxEdge northSide = Edge(G, 2, 1, 2, 2);
            GrayboxEdge eastSide = Edge(G, 2, 1, 3, 1);

            Assert.IsTrue(northSide.RunsAlongX);
            Assert.AreEqual(2.5f, northSide.CenterCellX);
            Assert.AreEqual(2f, northSide.CenterCellZ);
            Assert.IsFalse(eastSide.RunsAlongX);
            Assert.AreEqual(3f, eastSide.CenterCellX);
            Assert.AreEqual(1.5f, eastSide.CenterCellZ);
        }

        [Test]
        public void Edge_RejectsCellsThatAreNotNeighbours()
        {
            Assert.Throws<ArgumentException>(() => GrayboxEdge.Between(G, C(0, 0), C(1, 1)));
            Assert.Throws<ArgumentException>(() => GrayboxEdge.Between(G, C(0, 0), C(2, 0)));
            Assert.Throws<ArgumentException>(() => GrayboxEdge.Between(G, C(0, 0), C(0, 0)));
        }

        [Test]
        public void Rect_ListsEveryCellOnce()
        {
            var rect = new GrayboxRect(-1, 2, 3, 2);
            var cells = new List<GrayboxCell>();

            rect.AppendCellsTo(cells);

            Assert.AreEqual(6, rect.CellCount);
            Assert.AreEqual(6, cells.Count);
            Assert.AreEqual(C(-1, 2), cells[0]);
            Assert.AreEqual(C(1, 3), cells[5]);
            Assert.IsTrue(rect.Contains(C(1, 3)));
            Assert.IsFalse(rect.Contains(C(2, 3)));
            Assert.AreEqual(0, new GrayboxRect(0, 0, 0, 4).CellCount);
        }

        [TestCase(GrayboxDirection.North, 180f, 0, 1)]
        [TestCase(GrayboxDirection.East, 270f, 1, 0)]
        [TestCase(GrayboxDirection.South, 0f, 0, -1)]
        [TestCase(GrayboxDirection.West, 90f, -1, 0)]
        public void Stairs_ModelYawMakesEnvStairsClimbTowardsTheDirection(GrayboxDirection direction, float expectedYaw, int dx, int dz)
        {
            var stairs = new GrayboxStairs(C(2, 1), direction);

            Assert.AreEqual(expectedYaw, stairs.ModelYawDegrees, 0.001f);

            // Env_Stairs climbs towards local -Z. Yaw (about +Y) sends local +Z to (sin, cos), so -Z goes to (-sin, -cos).
            double yaw = stairs.ModelYawDegrees * Math.PI / 180.0;
            Assert.AreEqual(dx, Math.Round(-Math.Sin(yaw)), direction.ToString());
            Assert.AreEqual(dz, Math.Round(-Math.Cos(yaw)), direction.ToString());
            Assert.AreEqual(direction.DeltaX(), dx);
            Assert.AreEqual(direction.DeltaZ(), dz);
        }

        [Test]
        public void Stairs_OneCellRunCoversEntranceStartAndArrival()
        {
            var stairs = new GrayboxStairs(C(2, 1), GrayboxDirection.North);

            Assert.AreEqual(1, GrayboxStairs.RunCells, "Env_Stairs is used unscaled: 4 m run, 4 m rise");
            Assert.AreEqual(1, stairs.Cells.Count);
            Assert.AreEqual(C(2, 1), stairs.LastCell);
            Assert.AreEqual(C(2, 0), stairs.Entrance);
            Assert.AreEqual(C(2, 2), stairs.Arrival);
            Assert.AreEqual(Edge(G, 2, 0, 2, 1), stairs.EntranceEdge);
            Assert.AreEqual(Edge(U, 2, 1, 2, 2), stairs.ArrivalEdge);
            Assert.IsTrue(stairs.Contains(C(2, 1)));
            Assert.IsFalse(stairs.Contains(C(2, 2)));
        }

        // ------------------------------------------------------------------------------------------------
        // ASCII map
        // ------------------------------------------------------------------------------------------------

        [TestCase(G)]
        [TestCase(U)]
        public void AsciiMap_IsNotEmptyAndHasTheExpectedShape(int floor)
        {
            GrayboxLayout layout = GrayboxLayout.Default;
            int minX, minZ, maxX, maxZ;
            layout.GetCellBounds(out minX, out minZ, out maxX, out maxZ);

            string map = GrayboxAsciiMap.Render(layout, floor);

            Assert.IsFalse(string.IsNullOrWhiteSpace(map));
            string[] lines = map.TrimEnd('\n').Split('\n');
            Assert.AreEqual((maxZ - minZ + 1) * 2 + 1, lines.Length, "two text rows per cell row plus the closing border");
            for (int i = 0; i < lines.Length; i++)
            {
                Assert.AreEqual((maxX - minX + 1) * 4 + 1, lines[i].Length, "line " + i);
            }
        }

        [Test]
        public void AsciiMap_GroundFloorShowsRoomsFrontDoorGateAndFence()
        {
            string map = GrayboxAsciiMap.Render(GrayboxLayout.Default, G);

            foreach (char symbol in new[] { '.', 'A', 'H', 'M', 'O', 'K', 'S' })
            {
                StringAssert.Contains(symbol.ToString(), map);
            }
            StringAssert.Contains("-F-", map);
            StringAssert.Contains("-G-", map);
            StringAssert.Contains("~", map);
            Assert.AreEqual(1, CountChar(map, 'F'), "exactly one front door");
            Assert.AreEqual(1, CountChar(map, 'G'), "exactly one stage gate");
        }

        [Test]
        public void AsciiMap_UpperFloorShowsRoomsRailingsAndTheShaftArrow()
        {
            string map = GrayboxAsciiMap.Render(GrayboxLayout.Default, U);

            foreach (char symbol in new[] { 'L', '1', '2', 'C', 'B', 'W' })
            {
                StringAssert.Contains(symbol.ToString(), map);
            }
            StringAssert.Contains("^", map);
            StringAssert.Contains("=", map);
            int shafts = 0;
            for (int i = 0; i < GrayboxLayout.Default.Stairs.Count; i++)
            {
                shafts += GrayboxLayout.Default.Stairs[i].TopFloor == U ? 1 : 0;
            }
            Assert.AreEqual(shafts * GrayboxStairs.RunCells, CountChar(map, '^'), "one arrow per shaft cell");
            Assert.AreEqual(0, CountChar(map, 'F'));
            Assert.AreEqual(0, CountChar(map, '~'), "no fence upstairs");
        }

        [Test]
        public void AsciiMap_DoesNotThrowOnTheMiniHouse()
        {
            Parts p = Parts.Mini();

            Assert.IsNotEmpty(GrayboxAsciiMap.Render(p.Build(), G));
            Assert.IsNotEmpty(GrayboxAsciiMap.Render(p.Build(), U));
        }

        [Test]
        public void AsciiLegend_ListsEveryRoom()
        {
            GrayboxLayout layout = GrayboxLayout.Default;

            string legend = GrayboxAsciiMap.Legend(layout);

            string[] lines = legend.TrimEnd('\n').Split('\n');
            Assert.AreEqual(layout.Rooms.Count, lines.Length);
            for (int i = 0; i < layout.Rooms.Count; i++)
            {
                StringAssert.Contains(layout.Rooms[i].Id, lines[i]);
                StringAssert.StartsWith(layout.Rooms[i].Symbol.ToString(), lines[i]);
            }
        }

        private static int CountChar(string text, char c)
        {
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                count += text[i] == c ? 1 : 0;
            }
            return count;
        }
    }
}
