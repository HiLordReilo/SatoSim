using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq.Expressions;
using Microsoft.Xna.Framework;

namespace SatoSim.Core.Data;

public class ChartData
{
    // Format
    public int FormatVersion { get; private set; }
    
    // Song data
    public float InitialBPM { get; set; }
    public uint SongLength { get; set; }
    public uint ASSUMPTION1_SongOffset { get; set; }
    /// <summary>
    /// Total count of all objects in the chart.
    /// </summary>
    public ushort ObjectCount { get; set; }
    public ushort EventCount { get; set; }
    /// <summary>
    /// Count of Note objects in the chart. Doesn't include Stream point objects.
    /// </summary>
    public ushort NoteCount { get; set; }

    public ChartObject[] ChartObjects;
    public List<ChartEvent> ChartEvents;
    
    
    
    public class ChartObject
    {
        public int Time { get; set; }
        public int TimeOffset { get; set; }
        public short ObjectID { get; set; }
        public short ParentID { get; set; }
        public short ChildrenObjects { get; set; }
        public List<short> ChildrenID { get; set; }
        public byte Unknown1 { get; set; }
        public bool FreeformStreamFlag { get; set; }
        public bool SlashFlag { get; set; }
        public bool HoldFlag { get; set; }
        public ushort Duration { get; set; }
        public ushort Position1 { get; set; }
        public ushort Position2 { get; set; }
        public ushort Unknown2 { get; set; }
        public uint ObjectModifier { get; set; }
        public byte TimeSnap { get; set; }
        public byte Unknown3 { get; set; }
        public ushort Unknown4 { get; set; }
        public uint Unknown5 { get; set; }
        
        // Gameplay-specific values
        public bool SimultaneousColor;
        public bool SimultaneousHighlighting;
        public bool ObjectPassed;
        public bool ObjectPassedCompletely;

        public int ObjectTime => Time + TimeOffset;

        public byte ObjectType
        {
            get
            {
                if (FreeformStreamFlag)
                    return 4; // Freeform Stream point
                if (SlashFlag)
                    if (HoldFlag)
                    {
                        return 3; // Standard Stream
                    }
                    else
                        return 1; // Slash note
                
                if (Position1 >= 8)
                    return 2; // Ripple note
                
                return 0; // Tap/Long note
            }
        }

        public ushort StandardStreamPrefab
        {
            get => (ushort)(Position1 + 8 * ((Position2 & 0b1111_1111_1111_1110) / 2));
            set
            {
                Position1 = (ushort)(value % 8);
                StreamPointPosY = (ushort)((value / 8));
            }
        }

        public bool StreamContinues
        {
            get => ChildrenObjects != 0;
            set
            {
                if (value)
                {
                    ChildrenObjects = 1;
                    ChildrenID = new List<short>() { ObjectID };
                }
                else
                {
                    ChildrenObjects = 0;
                    ChildrenID.Clear();
                }
            }
        }
        
        public short NextID
        {
            get => (short)(StreamContinues && ChildrenID.Count > 0 ? ChildrenID[0] : -1);
            set
            {
                if (value == -1 || ChildrenID.Count == 0)
                    StreamContinues = false;
                else
                    ChildrenID[0] = value;
            }
        }
        
        public bool AnchoredStreamPoint
        {
            get => Position2 == 0;
            set => Position2 = (ushort)(value ? 0 : 2);
        }
        
        public ushort StreamPointPosX
        {
            get => (ushort)(Position1 + 1);
            set => Position1 = (ushort)(value - 1);
        }

        public ushort StreamPointPosY
        {
            get
            {
                ushort bitPos = 0;
                ushort posY = Position2;
                while ((posY >>= 1) != 0) bitPos++;
                return bitPos;
            }
            set => Position2 = (ushort)Math.Pow(2, value);
        }
    }
    
    public class ExtendedChartObject : ChartObject
    {
        public int UnknownEx1 { get; set; }
        public int UnknownEx2 { get; set; }
        public short UnknownEx3 { get; set; }
        public short UnknownEx4 { get; set; }

        public ExtendedChartObject(ChartObject baseObject)
        {
            Time = baseObject.Time;
            TimeOffset = baseObject.TimeOffset;
            ObjectID = baseObject.ObjectID;
            ParentID = baseObject.ParentID;
            ChildrenObjects = baseObject.ChildrenObjects;
            ChildrenID = baseObject.ChildrenID;
            Unknown1 = baseObject.Unknown1;
            FreeformStreamFlag = baseObject.FreeformStreamFlag;
            SlashFlag = baseObject.SlashFlag;
            HoldFlag = baseObject.HoldFlag;
            Duration = baseObject.Duration;
            Position1 = baseObject.Position1;
            Position2 = baseObject.Position1;
            Unknown2 = baseObject.Unknown2;
            ObjectModifier = baseObject.ObjectModifier;
            TimeSnap = baseObject.TimeSnap;
            Unknown3 = baseObject.Unknown3;
            Unknown4 = baseObject.Unknown4;
            Unknown5 = baseObject.Unknown5;
        }
    }
    
    public class ChartEvent
    {
        public ushort EventType { get; set; }
        public ushort EventID { get; set; }
        public uint Time { get; set; }
        public long Unknown1 { get; set; }
        public float Data { get; set; }
        public long Unknown2_HI { get; set; }
        public long Unknown2_LO { get; set; }
    }



    public static ChartData ParsePLY(byte[] data)
    {
        // Prepare binary reader
        using BinaryReader reader = new BinaryReader(new MemoryStream(data));
        
        // Parse the header
        // Check format magic
        string magic = String.Empty;
        for (int i = 0; i < 4; i++)
        {
            magic += reader.ReadChar(); // 1 byte x 4 chars
        }
        
        Console.WriteLine(magic);

        if (magic.Equals("RBFF"))
            Console.WriteLine("Format magic check passed, begin parsing....");
        else
        {
            Console.WriteLine("Found incompatible format magic! Parsing aborted.");
            reader.Close();
            return null;
        }

        ChartData chart = new ChartData();

        chart.FormatVersion = reader.ReadInt32(); // 4 bytes
        if(chart.FormatVersion != 0x0D) Console.WriteLine("Detected unknown format version: " + chart.FormatVersion);

        reader.ReadBytes(8); // 8 bytes padding
        
        chart.InitialBPM = reader.ReadSingle(); // 4 bytes
        chart.SongLength = reader.ReadUInt32(); // 4 bytes
        chart.ASSUMPTION1_SongOffset = reader.ReadUInt32(); // 4 bytes
        if (chart.ASSUMPTION1_SongOffset != 0)
            Console.WriteLine("Assumed offset value is detected to be non-zero: " + chart.ASSUMPTION1_SongOffset);
        
        chart.ObjectCount = reader.ReadUInt16(); // 2 bytes
        chart.EventCount = reader.ReadUInt16(); // 2 bytes
        chart.NoteCount = reader.ReadUInt16(); // 2 bytes

        reader.ReadBytes(10); // 10 bytes padding

        chart.ChartObjects = new ChartObject[chart.ObjectCount];
        
        // Parse objects
        for (int i = 0; i < chart.ObjectCount; i++)
        {
            ChartObject obj = new ChartObject();

            obj.Time = reader.ReadInt32(); // 4 bytes
            obj.TimeOffset = reader.ReadInt32(); // 4 bytes
            obj.ObjectID = reader.ReadInt16(); // 2 bytes
            if (obj.ObjectID != i)
                Console.WriteLine("ObjectID (" + obj.ObjectID + ") does not match its position in the list (" + i +
                                  ")!");
            obj.ParentID = reader.ReadInt16(); // 2 bytes
            obj.ChildrenObjects = reader.ReadInt16(); // 2 bytes
            obj.ChildrenID = new List<short>();
            if (obj.ChildrenObjects > 0)
            {
                for (int c = 0; c < obj.ChildrenObjects; c++) obj.ChildrenID.Add(reader.ReadInt16()); // 2 bytes
            }

            obj.Unknown1 = reader.ReadByte(); // 1 byte
            obj.FreeformStreamFlag = reader.ReadBoolean(); // 1 byte
            obj.SlashFlag = reader.ReadBoolean(); // 1 byte
            obj.HoldFlag = reader.ReadBoolean(); // 1 byte
            obj.Duration = reader.ReadUInt16(); // 2 bytes
            obj.Position1 = reader.ReadUInt16(); // 2 bytes
            obj.Position2 = reader.ReadUInt16(); // 2 bytes
            obj.Unknown2 = reader.ReadUInt16(); // 2 bytes
            obj.ObjectModifier = reader.ReadUInt32(); // 4 bytes, a bitfield
            obj.TimeSnap = reader.ReadByte(); // 1 byte
            obj.Unknown3 = reader.ReadByte(); // 1 byte
            obj.Unknown4 = reader.ReadUInt16(); // 2 byte
            obj.Unknown5 = reader.ReadUInt32(); // 4 byte
            if ((obj.ObjectModifier & 8) == 8)
            {
                Console.WriteLine("Detected ObjectType & 8 in ID: " + obj.ObjectID +
                                  "\nUsing extended variant of chart object.");
                obj = new ExtendedChartObject(obj);
                ((ExtendedChartObject)obj).UnknownEx1 = reader.ReadInt32(); // 4 bytes
                ((ExtendedChartObject)obj).UnknownEx2 = reader.ReadInt32(); // 4 bytes
                ((ExtendedChartObject)obj).UnknownEx3 = reader.ReadInt16(); // 2 bytes
                ((ExtendedChartObject)obj).UnknownEx4 = reader.ReadInt16(); // 2 bytes
            }
            
            chart.ChartObjects[i] = obj;
        }
        
        
        // Parse events
        chart.ChartEvents = new List<ChartEvent>();

        bool httActive = false;
        
        for (int i = 0; i < chart.EventCount; i++)
        {
            ChartEvent ev = new ChartEvent();

            ev.EventType = reader.ReadUInt16();
            ev.EventID = reader.ReadUInt16();
            if (ev.EventID != i)
                Console.WriteLine("EventID (" + ev.EventID + ") does not match its position in the list (" + i + ")!");
            ev.Time = reader.ReadUInt32();
            ev.Unknown1 = reader.ReadInt64();
            ev.Data = reader.ReadSingle();
            ev.Unknown2_HI = reader.ReadInt64();
            ev.Unknown2_LO = reader.ReadInt64();
            
            // Let's also check if there's anything funky going on with events cuz why not
            // First let's see if it's of an undocumented type
            if (ev.EventType is not 5 and not 3)
                Console.WriteLine("Undocumented event type detected (" + ev.EventType + ") in EventID: " + ev.EventID);
            // Then, if it's a type already documented....
            else
            {
                // Let's check for other peculiarities
                if (ev.EventType == 5)
                {
                    if (ev.Data is 0f or 1f)
                    {
                        if ((ev.Data == 0f) == httActive)
                            Console.WriteLine("Repeated HIGH TENSION TIME state data for HTT event in EventID: " +
                                              ev.EventID);
                        
                        httActive = ev.Data == 0f;
                    }
                    else
                        Console.WriteLine("Unexpected HIGH TENSION TIME event data in EventID: " + ev.EventID);
                }
            }
            
            chart.ChartEvents.Add(ev);
        }
        
        reader.Close();
        
        //Utils.UpdateSimultaneousHighlights(ref chart.ChartObjects);
        
        return chart;
    }
}
