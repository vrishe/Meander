using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meander.Signals;

internal interface ISignalEvaluator
{
    double Evaluate(double t);
}
