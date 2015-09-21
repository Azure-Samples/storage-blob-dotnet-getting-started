using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BlobStorage
{
    public class ReflectionTools
    {
        public static void PrintTypeProperties(Object obj)
        {
            Console.WriteLine("Properties for {0} object:", obj.GetType().ToString());

            foreach (PropertyInfo prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                //handle list type
                Console.WriteLine(prop.PropertyType);
                object value = prop.GetValue(obj, new object[] { });
                ObsoleteAttribute obsoleteAttribute = prop.GetCustomAttribute<System.ObsoleteAttribute>(true);

                if (obsoleteAttribute == null)
                {
                    Console.WriteLine("\t{0}: {1}", prop.Name, value);
                }
                else
                {
                    Console.WriteLine("\t{0} [Obsolete]", prop.Name);
                }
            }
            Console.WriteLine();
        }

    }
}
