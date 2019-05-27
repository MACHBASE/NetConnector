using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Mach.Data.MachClient;

namespace Mach.Data.MachClient
{
	public sealed class MachParameterCollection : DbParameterCollection, IEnumerable<MachParameter>
	{
		internal MachParameterCollection()
		{
			m_parameters = new List<MachParameter>();
			m_nameToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}

		public MachParameter Add(string parameterName, DbType dbType)
		{
			MachParameter parameter = new MachParameter
			{
				ParameterName = parameterName,
				DbType = dbType,
			};
			AddParameter(parameter);
			return parameter;
		}

		public override int Add(object value)
		{
			AddParameter((MachParameter) value);
			return m_parameters.Count - 1;
		}

		public override void AddRange(Array values)
		{
			foreach (var obj in values)
				Add(obj);
		}

		public MachParameter AddWithValue(string parameterName, object value)
		{
			var parameter = new MachParameter
			{
				ParameterName = parameterName,
				Value = value
			};
			AddParameter(parameter);
			return parameter;
		}

		public override bool Contains(object value) => m_parameters.Contains((MachParameter) value);

		public override bool Contains(string value) => IndexOf(value) != -1;

		public override void CopyTo(Array array, int index) => throw new NotSupportedException();

		public override void Clear()
		{
			m_parameters.Clear();
			m_nameToIndex.Clear();
		}

		public override IEnumerator GetEnumerator() => m_parameters.GetEnumerator();

		IEnumerator<MachParameter> IEnumerable<MachParameter>.GetEnumerator() => m_parameters.GetEnumerator();

		protected override DbParameter GetParameter(int index) => m_parameters[index];

		protected override DbParameter GetParameter(string parameterName)
		{
			var index = IndexOf(parameterName);
			if (index == -1)
				throw new ArgumentException(String.Format("Parameter '{0}' not found in the collection", parameterName), nameof(parameterName));
			return m_parameters[index];
		}

		public override int IndexOf(object value) => m_parameters.IndexOf((MachParameter) value);

		public override int IndexOf(string parameterName)
		{
			var index = NormalizedIndexOf(parameterName);
			if (index == -1)
				return -1;
			return string.Equals(parameterName, m_parameters[index].ParameterName, StringComparison.OrdinalIgnoreCase) ? index : -1;
		}

		// Finds the index of a parameter by name, regardless of whether 'parameterName' or the matching
		// MachParameter.ParameterName has a leading '?' or '@'.
		internal int NormalizedIndexOf(string parameterName)
		{
			var normalizedName = MachParameter.NormalizeParameterName(parameterName ?? throw new ArgumentNullException(nameof(parameterName)));
			return m_nameToIndex.TryGetValue(normalizedName, out var index) ? index : -1;
		}

		public override void Insert(int index, object value) => m_parameters.Insert(index, (MachParameter) value);

		public override void Remove(object value) => RemoveAt(IndexOf(value));

		public override void RemoveAt(int index)
		{
			var oldParameter = m_parameters[index];
			if (oldParameter.NormalizedParameterName != null)
				m_nameToIndex.Remove(oldParameter.NormalizedParameterName);
			m_parameters.RemoveAt(index);

			foreach (var pair in m_nameToIndex.ToList())
			{
				if (pair.Value > index)
					m_nameToIndex[pair.Key] = pair.Value - 1;
			}
		}

		public override void RemoveAt(string parameterName) => RemoveAt(IndexOf(parameterName));

		protected override void SetParameter(int index, DbParameter value)
		{
			var newParameter = (MachParameter) value;
			var oldParameter = m_parameters[index];
			if (oldParameter.NormalizedParameterName != null)
				m_nameToIndex.Remove(oldParameter.NormalizedParameterName);
			m_parameters[index] = newParameter;
			if (newParameter.NormalizedParameterName != null)
				m_nameToIndex.Add(newParameter.NormalizedParameterName, index);
		}

		protected override void SetParameter(string parameterName, DbParameter value) => SetParameter(IndexOf(parameterName), value);

		public override int Count => m_parameters.Count;

		public override object SyncRoot => throw new NotSupportedException();

        public override bool IsFixedSize => false;
        public override bool IsReadOnly => false;
        public override bool IsSynchronized => false;

        public new MachParameter this[int index]
		{
			get => m_parameters[index];
			set => SetParameter(index, value);
		}

		public new MachParameter this[string name]
		{
			get => (MachParameter) GetParameter(name);
			set => SetParameter(name, value);
		}

		private void AddParameter(MachParameter parameter)
		{
			m_parameters.Add(parameter);
			if (parameter.NormalizedParameterName != null)
				m_nameToIndex[parameter.NormalizedParameterName] = m_parameters.Count - 1;
		}

		readonly List<MachParameter> m_parameters;
		readonly Dictionary<string, int> m_nameToIndex;
	}
}
