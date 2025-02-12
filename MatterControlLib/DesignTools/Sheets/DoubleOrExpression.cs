﻿/*
Copyright (c) 2019, Lars Brubaker, John Lewin
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using System;
using System.ComponentModel;
using MatterHackers.Agg;
using MatterHackers.DataConverters3D;

namespace MatterHackers.MatterControl.DesignTools
{

	[TypeConverter(typeof(DoubleOrExpression))]
	public class DoubleOrExpression : IDirectOrExpression
	{
		/// <summary>
		/// Is the expression referencing a cell in the table or an equation. If not it is simply a constant
		/// </summary>
		public bool IsEquation { get => Expression.Length > 0 && Expression[0] == '='; }

		public string Expression { get; set; }

		public override string ToString() => Expression;

		public double Value(IObject3D owner)
		{
			return SheetObject3D.EvaluateExpression<double>(owner, Expression);
		}

		public string ValueString(IObject3D owner)
		{
			return SheetObject3D.EvaluateExpression<string>(owner, Expression);
		}

		public DoubleOrExpression(double value)
		{
			Expression = value.ToString();
		}

		public DoubleOrExpression(string expression)
		{
			Expression = expression;
		}

		public static implicit operator DoubleOrExpression(double value)
		{
			return new DoubleOrExpression(value);
		}

		public static implicit operator DoubleOrExpression(string expression)
		{
			return new DoubleOrExpression(expression);
		}

		/// <summary>
		/// Evaluate the expression clap the result and return the clamped value.
		/// If the expression as not an equation, modify it to be the clamped value.
		/// </summary>
		/// <param name="item">The Object to find the table relative to</param>
		/// <param name="min">The min value to clamp to</param>
		/// <param name="max">The max value to clamp to</param>
		/// <param name="valuesChanged">Did the value actual get changed (clamped).</param>
		/// <returns></returns>
		public double ClampIfNotCalculated(IObject3D item, double min, double max, ref bool valuesChanged)
		{
			var value = agg_basics.Clamp(this.Value(item), min, max, ref valuesChanged);
			if (!this.IsEquation)
			{
				// clamp the actual expression as it is not an equation
				Expression = value.ToString();
			}

			return value;
		}

		public double DefaultAndClampIfNotCalculated(IObject3D item,
			double min,
			double max,
			string keyName,
			double defaultValue,
			ref bool changed)
		{
			var currentValue = this.Value(item);
			if (!this.IsEquation)
			{
				double databaseValue = UserSettings.Instance.Fields.GetDouble(keyName, defaultValue);

				if (currentValue == 0)
				{
					currentValue = databaseValue;
					changed = true;
				}

				currentValue = agg_basics.Clamp(currentValue, min, max, ref changed);

				Expression = currentValue.ToString();
			}

			return currentValue;
		}
	}
}