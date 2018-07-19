using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.IO;
using Edu.Stanford.Nlp.Util;
using Edu.Stanford.Nlp.Util.Logging;
using Java.IO;
using Java.Lang;
using Java.Util.Function;
using Sharpen;

namespace Edu.Stanford.Nlp.Stats
{
	/// <summary>
	/// A
	/// <c>DataSeries</c>
	/// represents a named sequence of
	/// <c>double</c>
	/// values, and optionally refers to another
	/// <c>DataSeries</c>
	/// as its
	/// domain.  Originally designed for making graphs and charts, but probably other
	/// uses could be found.
	/// This file also contains several
	/// <c>DataSeries</c>
	/// implementations as
	/// nested static classes:
	/// <li>
	/// <c>FunctionDataSeries</c>
	/// , which computes data series values
	/// dynamically, according to a function supplied at construction; </li>
	/// <li>
	/// <c>ArrayDataSeries</c>
	/// , which is backed by an array; </li>
	/// <li>
	/// <c>ListDataSeries</c>
	/// , which is backed by a list, and includes
	/// static methods for reading a data series from a file or input stream; and
	/// </li>
	/// <li>
	/// <c>AverageDataSeries</c>
	/// , which computes data series values
	/// dynamically as a linear combination of the value of other data series
	/// supplied at construction. </li>
	/// </summary>
	/// <author>Bill MacCartney</author>
	public interface IDataSeries
	{
		string Name();

		double Get(int i);

		// SAFE! if index out of bounds, return (double) i
		int Size();

		IDataSeries Domain();

		public abstract class AbstractDataSeries : IDataSeries
		{
			private string name;

			private IDataSeries domain;

			// can be null; then domain = 0, 1, 2, ...
			// .......................................................................
			public virtual string Name()
			{
				return name;
			}

			public virtual void SetName(string name)
			{
				this.name = name;
			}

			public virtual IDataSeries Domain()
			{
				return domain;
			}

			public virtual void SetDomain(IDataSeries domain)
			{
				this.domain = domain;
			}

			public virtual IList<Pair<double, double>> ToListPairDouble()
			{
				IList<Pair<double, double>> list = new List<Pair<double, double>>();
				for (int i = 0; i < Size(); i++)
				{
					double x = (Domain() != null ? Domain().Get(i) : (double)i);
					double y = Get(i);
					list.Add(new Pair<double, double>(x, y));
				}
				return list;
			}

			public abstract double Get(int arg1);

			public abstract int Size();
		}

		public class FunctionDataSeries : DataSeries.AbstractDataSeries
		{
			private IToIntFunction<object> sizeFn;

			private IIntToDoubleFunction function;

			public FunctionDataSeries(string name, IIntToDoubleFunction function, IToIntFunction<object> sizeFn, IDataSeries domain)
			{
				// .......................................................................
				SetName(name);
				this.function = function;
				this.sizeFn = sizeFn;
				SetDomain(domain);
			}

			public FunctionDataSeries(string name, IIntToDoubleFunction function, IToIntFunction<object> sizeFn)
				: this(name, function, sizeFn, null)
			{
			}

			public FunctionDataSeries(string name, IIntToDoubleFunction function, int size, IDataSeries domain)
				: this(name, function, ConstantSizeFn(size), domain)
			{
			}

			public FunctionDataSeries(string name, IIntToDoubleFunction function, int size)
				: this(name, function, size, null)
			{
			}

			public override double Get(int i)
			{
				if (i < 0 || i >= Size())
				{
					return i;
				}
				return function.ApplyAsDouble(i);
			}

			public override int Size()
			{
				return sizeFn.ApplyAsInt(null);
			}

			private static IToIntFunction<object> ConstantSizeFn(int size)
			{
				return null;
			}
		}

		public class ArrayDataSeries : DataSeries.AbstractDataSeries
		{
			private double[] data;

			public ArrayDataSeries(string name)
			{
				// .......................................................................
				SetName(name);
				SetData(new double[0]);
			}

			public ArrayDataSeries(string name, double[] data)
				: this(name)
			{
				SetData(data);
			}

			public ArrayDataSeries(string name, double[] data, IDataSeries domain)
				: this(name, data)
			{
				SetDomain(domain);
			}

			public virtual double[] Data()
			{
				return data;
			}

			public virtual void SetData(double[] data)
			{
				if (data == null)
				{
					throw new ArgumentNullException();
				}
				this.data = data;
			}

			public override double Get(int i)
			{
				if (i < 0 || i >= data.Length)
				{
					return i;
				}
				return data[i];
			}

			public virtual void Set(int i, double x)
			{
				if (i < 0 || i >= data.Length)
				{
					return;
				}
				// no-op
				data[i] = x;
			}

			public override int Size()
			{
				return data.Length;
			}
		}

		public class ListDataSeries : DataSeries.AbstractDataSeries
		{
			private IList<double> data;

			public ListDataSeries(string name)
			{
				// .......................................................................
				SetName(name);
				SetData(new List<double>());
			}

			public ListDataSeries(string name, IList<double> data)
				: this(name)
			{
				SetData(data);
			}

			public ListDataSeries(string name, IList<double> data, IDataSeries domain)
				: this(name, data)
			{
				SetDomain(domain);
			}

			public ListDataSeries(string name, IDataSeries domain)
				: this(name)
			{
				SetDomain(domain);
			}

			public virtual IList<double> Data()
			{
				return data;
			}

			public virtual void SetData(IList<double> data)
			{
				if (data == null)
				{
					throw new ArgumentNullException();
				}
				this.data = data;
			}

			public override double Get(int i)
			{
				if (i < 0 || i >= data.Count)
				{
					return i;
				}
				return data[i];
			}

			public virtual void Set(int i, double x)
			{
				if (i < 0 || i >= data.Count)
				{
					return;
				}
				// no-op
				data.Set(i, x);
			}

			public virtual void Add(double x)
			{
				data.Add(x);
			}

			public override int Size()
			{
				return data.Count;
			}

			/// <summary>
			/// If a record contains a field that can't be parsed as a double, the whole
			/// record is skipped.
			/// </summary>
			public static IDataSeries[] ReadDataSeries(RecordIterator it, bool useHeaders)
			{
				if (!it.MoveNext())
				{
					return null;
				}
				IList<string> record = it.Current;
				// read first record
				int columns = record.Count;
				if (columns < 1)
				{
					throw new ArgumentException();
				}
				DataSeries.ListDataSeries[] serieses = new DataSeries.ListDataSeries[columns];
				for (int col = 0; col < columns; col++)
				{
					DataSeries.ListDataSeries series = new DataSeries.ListDataSeries("y" + col);
					if (col == 0)
					{
						series.SetName("x");
					}
					else
					{
						series.SetDomain(serieses[0]);
					}
					serieses[col] = series;
				}
				if (useHeaders)
				{
					// first record contains header strings
					for (int i = 0; i < record.Count && i < serieses.Length; i++)
					{
						serieses[i].SetName(record[i]);
					}
					record = it.Current;
				}
				while (true)
				{
					try
					{
						double[] values = new double[columns];
						for (int col_1 = 0; col_1 < columns; col_1++)
						{
							values[col_1] = double.ValueOf(record[col_1]);
						}
						for (int col_2 = 0; col_2 < columns; col_2++)
						{
							serieses[col_2].Add(values[col_2]);
						}
					}
					catch (NumberFormatException)
					{
					}
					// skip whole record
					if (!it.MoveNext())
					{
						break;
					}
					record = it.Current;
				}
				return serieses;
			}

			public static IDataSeries[] ReadDataSeries(InputStream @in, bool useHeaders)
			{
				return ReadDataSeries(new RecordIterator(@in), useHeaders);
			}

			public static IDataSeries[] ReadDataSeries(InputStream @in)
			{
				return ReadDataSeries(new RecordIterator(@in), false);
			}

			/// <exception cref="Java.IO.FileNotFoundException"/>
			public static IDataSeries[] ReadDataSeries(string filename, bool useHeaders)
			{
				return ReadDataSeries(new RecordIterator(filename), useHeaders);
			}

			/// <exception cref="Java.IO.FileNotFoundException"/>
			public static IDataSeries[] ReadDataSeries(string filename)
			{
				return ReadDataSeries(new RecordIterator(filename), false);
			}

			/// <exception cref="Java.IO.FileNotFoundException"/>
			public static void Main(string[] args)
			{
				IDataSeries[] serieses = null;
				if (args.Length > 0)
				{
					serieses = ReadDataSeries(args[0], true);
				}
				else
				{
					DataSeriesConstants.log.Info("[Reading from stdin...]");
					serieses = ReadDataSeries(Runtime.@in, true);
				}
				foreach (IDataSeries series in serieses)
				{
					System.Console.Out.Write(series.Name() + ": ");
					System.Console.Out.WriteLine(((DataSeries.ListDataSeries)series).ToListPairDouble());
				}
			}

			private static void Demo1()
			{
				DataSeries.ListDataSeries xData = new DataSeries.ListDataSeries("x");
				DataSeries.ListDataSeries yData = new DataSeries.ListDataSeries("y", xData);
				for (double x = 0.0; x < 5.0; x++)
				{
					xData.Add(x);
					yData.Add(x * x);
				}
				System.Console.Out.WriteLine(yData.ToListPairDouble());
			}
		}

		public class AverageDataSeries : IDataSeries
		{
			private IDataSeries[] components;

			public AverageDataSeries(IDataSeries[] components)
			{
				// .......................................................................
				if (components == null || components.Length < 1)
				{
					throw new ArgumentException("Need at least one component!");
				}
				this.components = new IDataSeries[components.Length];
				for (int i = 0; i < components.Length; i++)
				{
					if (components[i] == null)
					{
						throw new ArgumentException("Can't have null components!");
					}
					this.components[i] = components[i];
				}
				Domain();
			}

			// to ensure domains are same
			public virtual string Name()
			{
				StringBuilder name = new StringBuilder();
				name.Append("avg(");
				bool flag = false;
				foreach (IDataSeries series in components)
				{
					if (flag)
					{
						name.Append(", ");
					}
					else
					{
						flag = true;
					}
					name.Append(series.Name());
				}
				name.Append(")");
				return name.ToString();
			}

			public virtual double Get(int i)
			{
				double y = 0.0;
				foreach (IDataSeries series in components)
				{
					y += series.Get(i);
				}
				return y / components.Length;
			}

			public virtual int Size()
			{
				int size = int.MaxValue;
				foreach (IDataSeries series in components)
				{
					size = Math.Min(size, series.Size());
				}
				return size;
			}

			public virtual IDataSeries Domain()
			{
				IDataSeries domain = components[0].Domain();
				// could be null
				foreach (IDataSeries series in components)
				{
					if (series.Domain() != domain)
					{
						throw new InvalidOperationException("The components of this AverageDataSeries do not have the same domains!");
					}
				}
				return domain;
			}

			public override string ToString()
			{
				return Name();
			}
		}
	}

	public static class DataSeriesConstants
	{
		/// <summary>A logger for this class</summary>
		public const Redwood.RedwoodChannels log = Redwood.Channels(typeof(IDataSeries));
	}
}
