using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ayllu.Backend.Application.Serializers
{
    public static class MintRedeemer
    {
        /// <summary>
        /// Devuelve un redeemer de tipo Unit, representado como constructor 0 sin campos.
        /// </summary>
        public static object Build()
        {
            return new
            {
                constructor = 0,
                fields = Array.Empty<object>()
            };
        }
    }
}
