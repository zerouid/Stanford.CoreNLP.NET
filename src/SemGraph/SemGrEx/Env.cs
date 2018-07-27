using System;
using System.Collections.Generic;
using Edu.Stanford.Nlp.Ling;


namespace Edu.Stanford.Nlp.Semgraph.Semgrex
{
	/// <author>sonalg</author>
	/// <version>11/3/14.</version>
	[System.Serializable]
	public class Env
	{
		private const long serialVersionUID = -4168610545399833956L;

		/// <summary>Mapping of variable names to their values.</summary>
		private readonly IDictionary<string, object> variables;

		public Env()
		{
			variables = new Dictionary<string, object>();
		}

		public Env(IDictionary<string, object> variables)
		{
			this.variables = variables;
		}

		public virtual void Bind(string name, object obj)
		{
			if (obj != null)
			{
				variables[name] = obj;
			}
			else
			{
				Sharpen.Collections.Remove(variables, name);
			}
		}

		public virtual void Unbind(string name)
		{
			Bind(name, null);
		}

		public virtual object Get(string name)
		{
			return variables[name];
		}

		public static Type LookupAnnotationKey(Edu.Stanford.Nlp.Semgraph.Semgrex.Env env, string name)
		{
			if (env != null)
			{
				object obj = env.Get(name);
				if (obj != null)
				{
					if (obj is Type)
					{
						return (Type)obj;
					}
				}
			}
			//        else if (obj instanceof Value) {
			//          obj = ((Value) obj).get();
			//          if (obj instanceof Class) {
			//            return (Class) obj;
			//          }
			//        }
			Type coreKeyClass = AnnotationLookup.ToCoreKey(name);
			if (coreKeyClass != null)
			{
				return coreKeyClass;
			}
			else
			{
				try
				{
					Type clazz = Sharpen.Runtime.GetType(name);
					return clazz;
				}
				catch (TypeLoadException)
				{
					return null;
				}
			}
		}
	}
}
