using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GraphComponents.Models
{
    public interface ISerializable<TValue>
    {
        JObject Serialize(TValue value);
    }

    public interface IDeserializable<TValue>
    {
        TValue Deserialize(JObject json);
    }

    public interface IConnector<TIdentity, TValue> : INotifyPropertyChanged
        where TIdentity : IEquatable<TIdentity>
    {
        TIdentity Identity { get; }
        TValue Value { get; }
    }

    public interface IInputConnector<TIdentity, TValue> : INotifyPropertyChanged, IConnector<TIdentity, TValue>
        where TIdentity : IEquatable<TIdentity>
    {
        new TValue Value { get; set; }
    }

    public interface IOutputConnector<TIdentity, TValue> : INotifyPropertyChanged, IConnector<TIdentity, TValue>
        where TIdentity : IEquatable<TIdentity>
    {

    }

    public interface INode<TIdentity, TValue> : IInterpretable
        where TIdentity : IEquatable<TIdentity>
    {
        IEnumerable<IInputConnector<TIdentity, TValue>> InputConnectors { get; }
        IEnumerable<IOutputConnector<TIdentity, TValue>> OutputConnectors { get; }
    }

    public interface IEdge<TIdentity, TValue>
        where TIdentity : IEquatable<TIdentity>
    {
        IOutputConnector<TIdentity, TValue> Source { get; }
        IInputConnector<TIdentity, TValue> Target { get; }
    }

    public interface ICommand<TInput, TOutput>
    {
        TOutput Process(TInput input);
    }

    // I don't really know is this a code smell or not.
    public interface IInterpretable
    {
        void Evaluate();
    }

    public abstract class CommandNode<TInput, TOutput> : INode<string, JObject>, ICommand<TInput, TOutput>
        where TInput : ISerializable<TInput>, IDeserializable<TInput>, new()
        where TOutput : ISerializable<TOutput>, IDeserializable<TOutput>, new()
    {
        private LinkedList<CustomInputConnector> _inputConnectors;
        private LinkedList<CustomOutputConnector> _outputConnectors;

        public virtual IEnumerable<IInputConnector<string, JObject>> InputConnectors => _inputConnectors;

        public virtual IEnumerable<IOutputConnector<string, JObject>> OutputConnectors => _outputConnectors;

        public CommandNode()
        {
            _inputConnectors = new LinkedList<CustomInputConnector>();
            _outputConnectors = new LinkedList<CustomOutputConnector>();

            var tempInput = new TInput();
            foreach (var property in tempInput.Serialize(tempInput).Properties())
            {
                var identity = property.Name;
                var value = property.Value as JObject;
                _inputConnectors.AddLast(new CustomInputConnector(identity, value));
                
            }
            var tempOutput = new TOutput();
            foreach (var property in tempOutput.Serialize(tempOutput).Properties())
            {
                var identity = property.Name;
                var value = property.Value as JObject;
                _inputConnectors.AddLast(new CustomInputConnector(identity, value));
                _outputConnectors.AddLast(new CustomOutputConnector(identity, value));
            }
        }

        protected bool IsBasicType(Type type) => type.IsPrimitive || type == typeof(string) || type == typeof(object);

        public void Evaluate()
        {
            var inputJObject = new JObject();
            foreach (var inputConnector in _inputConnectors)
            {
                inputJObject[inputConnector.Identity] = inputConnector.Value;
            }

            // Step 2: Deserialize to TInput
            var inputInstance = new TInput().Deserialize(inputJObject);

            // Step 3: Process the input
            var outputInstance = Process(inputInstance);

            // Step 4: Serialize the output
            var outputJObject = outputInstance.Serialize(outputInstance);

            // Step 5: Set the output values
            foreach (var outputConnector in _outputConnectors)
            {
                if (outputJObject.TryGetValue(outputConnector.Identity, out JToken value))
                {
                    outputConnector.Value = (JObject)value;
                }
            }
        }

        public abstract TOutput Process(TInput input);

        private abstract class CustomConnector : IConnector<string, JObject>
        {
            public string Identity { get; protected set; }

            public JObject Value { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            public CustomConnector(string identity, JObject value)
            {
                this.Identity = identity;
                this.Value = value;
            }          

            public bool Equals(IConnector<string, JObject> other)
            {
                return this.Identity.Equals(other.Identity);
            }
        }

        private class CustomInputConnector : CustomConnector, IInputConnector<string, JObject>
        {
            public CustomInputConnector(string identity, JObject value) : base(identity, value) { }
        }

        private class CustomOutputConnector : CustomConnector, IOutputConnector<string, JObject>
        {
            public CustomOutputConnector(string identity, JObject value) : base(identity, value) { }
        }
    }

    public class Graph
    {
        public ObservableCollection<INode<string, JObject>> Nodes { get; protected set; }
        public ObservableCollection<IEdge<string, JObject>> Edges { get; protected set; }

        public Graph(IEnumerable<INode<string, JObject>> nodes, IEnumerable<IEdge<string, JObject>> edges)
        {
            var isAllEdgeConnectedWithNodes =
                edges.All(edge => nodes.Any(node => node.OutputConnectors.Any(outputConnector => outputConnector.Equals(edge.Source))))
                && edges.All(edge => nodes.Any(node => node.InputConnectors.Any(inputConnector => inputConnector.Equals(edge.Target))));
            if (!isAllEdgeConnectedWithNodes)
                throw new NotImplementedException();

            Nodes = new ObservableCollection<INode<string, JObject>>(nodes);
            Edges = new ObservableCollection<IEdge<string, JObject>>(edges);
        }
    }

    public class CommandGraphExecuter<TNode, TEdge>
        where TNode : INode<string, object>
        where TEdge : IEdge<string, object>
    {
        public void Execute(IEnumerable<TNode> nodes, IEnumerable<TEdge> edges)
        {
            var executedNodes = new List<TNode>();
            var unexecutedNodes = nodes.ToList();
            while (true)
            {
                var canBeExecutedNodes = unexecutedNodes
                    .Where(node => node.InputConnectors
                        .All(inputConnector =>
                            inputConnector.Value != null
                            || edges
                                .Any(edge => executedNodes
                                    .Any(executedNode => executedNode.OutputConnectors
                                        .Any(connector => connector.Equals(edge.Source))))));
                if (canBeExecutedNodes.Count() == 0)
                    break;
                else
                {
                    var tasks = new List<Task>();
                    foreach (var commandNode in canBeExecutedNodes)
                    {
                        tasks.Add(Task.Run(() => commandNode.Evaluate()));
                    }
                    Task.WaitAll(tasks.ToArray());
                    var newlyExecutedNodes = canBeExecutedNodes;
                    var temp = edges
                        .Where(edge => newlyExecutedNodes
                            .Any(newExecutedNode => newExecutedNode.OutputConnectors
                                .Any(connector => connector.Equals(edge.Source))));
                    foreach (var edge in temp)
                    {
                        edge.Target.Value = edge.Source.Value;
                    }
                }
            }
        }
    }

    public class TestData1 : ISerializable<TestData1>, IDeserializable<TestData1>
    {
        public double TestData1_Data1 { get; set; }

        public JObject Serialize(TestData1 value)
        {
            return JObject.FromObject(value);
        }

        public TestData1 Deserialize(JObject json)
        {
            return json.ToObject<TestData1>();
        }
    }

    public class TestData2 : ISerializable<TestData2>, IDeserializable<TestData2>
    {
        public int TestData2_Data1 { get; set; }
        public TestData1 TestData2_Data2 { get; set; }

        public JObject Serialize(TestData2 value)
        {
            return JObject.FromObject(value);
        }

        public TestData2 Deserialize(JObject json)
        {
            return json.ToObject<TestData2>();
        }
    }

    public class TestData3 : ISerializable<TestData3>, IDeserializable<TestData3>
    {
        public TestData1 TestData3_Data1 { get; set; }
        public TestData2 TestData3_Data2 { get; set; }

        public JObject Serialize(TestData3 value)
        {
            return JObject.FromObject(value);
        }

        public TestData3 Deserialize(JObject json)
        {
            return json.ToObject<TestData3>();
        }
    }

    public class TestData4 : ISerializable<TestData4>, IDeserializable<TestData4>
    {
        public TestData3 TestData4_Data1 { get; set; }

        public JObject Serialize(TestData4 value)
        {
            return JObject.FromObject(value);
        }

        public TestData4 Deserialize(JObject json)
        {
            return json.ToObject<TestData4>();
        }
    }

    public class SerializableWrapper<T> : ISerializable<SerializableWrapper<T>>, IDeserializable<SerializableWrapper<T>>
    {
        public T Value { get; set; }

        public SerializableWrapper() { }

        public SerializableWrapper(T value)
        {
            Value = value;
        }

        public JObject Serialize(SerializableWrapper<T> value)
        {
            return JObject.FromObject(new { value = value.Value });
        }

        public SerializableWrapper<T> Deserialize(JObject json)
        {
            var value = json["value"].ToObject<T>();
            return new SerializableWrapper<T>(value);
        }
    }

    public class TestData5 : ISerializable<TestData5>, IDeserializable<TestData5>
    {
        public TestData4 TestData5_Data1 { get; set; }

        public JObject Serialize(TestData5 value)
        {
            return JObject.FromObject(value);
        }

        public TestData5 Deserialize(JObject json)
        {
            return json.ToObject<TestData5>();
        }
    }
    public class TestImplement1Node : CommandNode<TestData3, SerializableWrapper<string>>
    {
        public override SerializableWrapper<string> Process(TestData3 input)
        {
            // Example implementation
            return new SerializableWrapper<string>($"Processed TestData3 with Data1: {input.TestData3_Data1.TestData1_Data1} and Data2: {input.TestData3_Data2.TestData2_Data1}");
        }
    }

    public class TestImplement2Node : CommandNode<SerializableWrapper<int>, TestData2>
    {
        public override TestData2 Process(SerializableWrapper<int> input)
        {
            // Example implementation
            return new TestData2
            {
                TestData2_Data1 = input.Value,
                TestData2_Data2 = new TestData1 { TestData1_Data1 = input.Value * 2 }
            };
        }
    }

    public class TestImplement3Node : TestImplement1Node
    {
    }

    public class TestImplement4Node : CommandNode<TestData5, TestData4>
    {
        public override TestData4 Process(TestData5 input)
        {
            // Example implementation
            return new TestData4
            {
                TestData4_Data1 = new TestData3
                {
                    TestData3_Data1 = new TestData1 { TestData1_Data1 = input.TestData5_Data1.TestData4_Data1.TestData3_Data1.TestData1_Data1 * 2 },
                    TestData3_Data2 = new TestData2
                    {
                        TestData2_Data1 = 100,
                        TestData2_Data2 = new TestData1 { TestData1_Data1 = 200 }
                    }
                }
            };
        }
    }

    public class TestImplement5Node : CommandNode<SerializableWrapper<int>, SerializableWrapper<double>>
    {
        public override SerializableWrapper<double> Process(SerializableWrapper<int> input)
        {
            // Example implementation
            return new SerializableWrapper<double>(Math.Sqrt(input.Value));
        }
    }

}