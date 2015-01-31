using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Xml.Serialization;
using LaunchPad2.ViewModels;
using NodeControl;
using Color = System.Drawing.Color;

namespace LaunchPad2.Models
{
    /// <summary>
    /// This class serves two purposes; provide XML serialization for packaging and binary serialization for the clipboard.
    /// </summary>
    [Serializable]
    [XmlType("Track")]
    public class TrackModel : ISerializable
    {
        private const string NameSerializationInfoKey = "name";
        private const string DeviceIdSerializationInfoKey = "device";
        private const string NodeIdSerializationInfoKey = "node";
        private const string PortSerializationInfoKey = "port";
        private const string ColorSerializationInfoKey = "color";
        private const string CuesSerializationInfoKey = "cues";
        private const string NotesSerializationInfoKey = "notes";

        public TrackModel()
        {
            Cues = new List<CueModel>();
        }

        public TrackModel(TrackViewModel viewModel) : this()
        {
            Name = viewModel.Name;

            if (viewModel.Device != null)
                DeviceId = viewModel.Device.Id;

            if (viewModel.Node != null)
                NodeId = viewModel.Node.Address.Value;

            if (viewModel.Port != null)
                Port = viewModel.Port.Port;

            var brush = viewModel.Brush as SolidColorBrush;
            if (brush != null)
            {
                System.Windows.Media.Color color = brush.Color;
                Color = Color.FromArgb(color.A, color.R, color.G, color.B);
            }

            foreach (EventCueViewModel cue in viewModel.Cues)
                Cues.Add(new CueModel(cue));

            Notes = viewModel.Notes;
        }

        public TrackModel(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString(NameSerializationInfoKey);
            DeviceId = info.GetString(DeviceIdSerializationInfoKey);
            NodeId = info.GetUInt64(NodeIdSerializationInfoKey);
            Port = (Ports)info.GetUInt64(PortSerializationInfoKey);
            ColorHex = info.GetString(ColorSerializationInfoKey);
            Cues = (List<CueModel>) info.GetValue(CuesSerializationInfoKey, typeof (List<CueModel>));
            Notes = info.GetString(NotesSerializationInfoKey);
        }

        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string DeviceId { get; set; }

        [XmlAttribute]
        public ulong NodeId { get; set; }

        [XmlAttribute]
        public Ports Port { get; set; }

        [XmlIgnore]
        public Color Color { get; set; }

        [XmlAttribute("Color")]
        public string ColorHex
        {
            get { return ColorTranslator.ToHtml(Color); }
            set { Color = ColorTranslator.FromHtml(value); }
        }

        public List<CueModel> Cues { get; set; }

        [XmlAttribute]
        public string Notes { get; set; }

        public TrackViewModel GetViewModel(IEnumerable<DeviceViewModel> deviceSource,
            IEnumerable<NodeViewModel> nodeSource)
        {
            NodeViewModel node = nodeSource.SingleOrDefault(n => n.Address.Value == NodeId);

            var brush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                Color.A, Color.R, Color.G, Color.B));

            return new TrackViewModel(Name, brush)
            {
                Cues = new ObservableCollection<EventCueViewModel>(Cues.Select(cue => cue.GetViewModel())),
                Device = deviceSource.SingleOrDefault(device => device.Id == DeviceId),
                Node = node,
                Port = node == null ? null : node.Ports.SingleOrDefault(port => port.Port == Port),
                Notes = Notes
            };
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(NameSerializationInfoKey, Name);
            info.AddValue(DeviceIdSerializationInfoKey, DeviceId);
            info.AddValue(NodeIdSerializationInfoKey, NodeId);
            info.AddValue(PortSerializationInfoKey, (ulong)Port);
            info.AddValue(ColorSerializationInfoKey, ColorHex);
            info.AddValue(CuesSerializationInfoKey, Cues);
            info.AddValue(NotesSerializationInfoKey, Notes);
        }
    }
}