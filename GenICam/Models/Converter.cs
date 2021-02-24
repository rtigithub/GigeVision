using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GenICam
{
    public class Converter : IMathematical
    {
        private Dictionary<string, object> PVariables { get; set; }
        private string FormulaFrom { get; set; }
        private string FormulaTo { get; set; }

        private Slope Slope { get; set; }

        public object PValue { get; private set; }

        public Task<double> Value
        {
            get
            {
                return ExecuteFormulaFrom();
            }
            set
            {
                Value = ExecuteFormulaTo();
            }
        }

        public Converter(string formulaTo, string formulaFrom, object pValue, Slope slope, Dictionary<string, object> pVariables = null)
        {
            FormulaTo = formulaTo;
            FormulaFrom = formulaFrom;
            PVariables = pVariables;
            PValue = pValue;
            Slope = slope;
        }

        private async Task<double> ExecuteFormulaFrom()
        {
            if (PValue != null)
            {
                var nullableValue = await ReadPValue(PValue);
                if (nullableValue != null)
                    FormulaFrom.Replace("To", nullableValue.ToString());
                else
                    throw new Exception("Failed to read register value", new InvalidDataException());
            }
            foreach (var word in FormulaFrom.Split())
            {
                foreach (var pVariable in PVariables)
                {
                    if (pVariable.Key.Equals(word))
                    {
                        string value = "";
                        var nullableValue = await ReadPValue(pVariable.Value);
                        if (nullableValue != null)
                            value = nullableValue.ToString();
                        else
                            throw new Exception("Failed to read register value", new InvalidDataException());

                        FormulaFrom = FormulaFrom.Replace(word, value);
                        break;
                    }
                }
            }

            return Evaluate(FormulaFrom);
        }

        private async Task<double> ExecuteFormulaTo()
        {
            if (PValue != null)
            {
                var nullableValue = await ReadPValue(PValue);
                if (nullableValue != null)
                    FormulaFrom.Replace("From", nullableValue.ToString());
                else
                    throw new Exception("Failed to read register value", new InvalidDataException());
            }
            foreach (var word in FormulaTo.Split())
            {
                foreach (var pVariable in PVariables)
                {
                    if (pVariable.Key.Equals(word))
                    {
                        string value = "";
                        var nullableValue = await ReadPValue(pVariable.Value);
                        if (nullableValue != null)
                            value = nullableValue.ToString();
                        else
                            throw new Exception("Failed to read register value", new InvalidDataException());

                        FormulaTo = FormulaTo.Replace(word, value);
                        break;
                    }
                }
            }

            return Evaluate(FormulaFrom);
        }

        private async Task<IConvertible> ReadPValue(object pValue)
        {
            //ToDo : Cover all cases
            if (pValue is GenInteger integer)
                return await integer.GetValue();
            else if (pValue is GenIntReg intReg)
                return await intReg.GetValue();
            else if (pValue is GenMaskedIntReg genMaskedIntReg)
                return await genMaskedIntReg.GetValue();
            else if (pValue is IntSwissKnife intSwissKnife1)
                return await intSwissKnife1.GetValue();
            else if (pValue is GenFloat genFloat)
                return await genFloat.GetValue();

            return null;
        }

        /// <summary>
        /// this method evaluate the formula expression
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        private double Evaluate(string expression)
        {
            expression = "( " + expression + " )";
            Stack<string> opreators = new Stack<string>();
            Stack<double> values = new Stack<double>();
            bool tempBoolean = false;
            bool isPower = false;
            bool isEqual = false;
            foreach (var word in expression.Split())
            {
                if (word.StartsWith("0x"))
                    values.Push(Int64.Parse(word.Substring(2), System.Globalization.NumberStyles.HexNumber));
                else if (double.TryParse(word, out double tempNumber))
                    values.Push(tempNumber);
                else
                {
                    switch (word)
                    {
                        case "*":
                            if (isPower)
                            {
                                opreators.Pop();
                                opreators.Push("**");
                                isPower = false;
                            }
                            else
                            {
                                isPower = true;
                                opreators.Push(word);
                            }
                            isEqual = false;
                            break;

                        case "(":
                        case "+":
                        case "-":
                        case "/":
                        case "=":
                            isEqual = true;
                            isPower = false;
                            opreators.Push(word);
                            break;

                        case "?":
                        case ":":
                        case "&":
                        case "|":
                        case ">":
                            if (isEqual)
                            {
                                opreators.Pop();
                                opreators.Push(">=");
                            }
                            else
                            {
                                opreators.Push(word);
                            }
                            isEqual = false;
                            isPower = false;
                            break;

                        case "<":
                            if (isEqual)
                            {
                                opreators.Pop();
                                opreators.Push("<=");
                            }
                            else
                            {
                                opreators.Push(word);
                            }
                            isEqual = false;
                            isPower = false;
                            break;

                        case "%":
                        case "^":
                        case "~":
                        case "ATAN":
                        case "COS":
                        case "SIN":
                        case "TAN":
                        case "ABS":
                        case "EXP":
                        case "LN":
                        case "LG":
                        case "SQRT":
                        case "TRUNC":
                        case "FLOOR":
                        case "CELL":
                        case "ROUND":
                        case "ASIN":
                        case "ACOS":
                        case "SGN":
                        case "NEG":
                        case "E":
                        case "PI":
                            opreators.Push(word);
                            isPower = false;
                            isEqual = false;
                            break;

                        case ")":
                            while (values.Count > 0 && opreators.Count > 0)
                            {
                                string opreator = opreators.Pop();

                                tempBoolean = DoMathOpreation(opreator, opreators, values);

                                if (opreator.Equals("?"))
                                {
                                    if (tempBoolean)
                                    {
                                        if (values.Count > 0)
                                            return values.Pop();
                                    }
                                }
                                if (opreators.Count > 0)
                                {
                                    if (opreators.Peek().Equals("("))
                                    {
                                        opreators.Pop();
                                        break;
                                    }
                                }
                            }
                            isPower = false;
                            isEqual = false;
                            break;

                        case "":

                            break;

                        default:
                            isPower = false;
                            isEqual = false;
                            break;
                    }
                }
            }

            if (values.Count > 0)
                return values.Pop();

            if (tempBoolean)
                return 1;
            else
                return 0;

            throw new InvalidDataException("Failed to read the formula");
        }

        private bool DoMathOpreation(string opreator, Stack<string> opreators, Stack<double> values)
        {
            bool tempBoolean = false;
            double value = 0;
            int integerValue = 0;
            //ToDo: Implement (&&) , (||) Operators

            if (opreator.Equals("+"))
            {
                value = (double)values.Pop();
                value = (double)values.Pop() + value;
                values.Push(value);
            }
            else if (opreator.Equals("-"))
            {
                value = (double)values.Pop();
                value = (double)values.Pop() - value;
                values.Push(value);
            }
            else if (opreator.Equals("*"))
            {
                value = (double)values.Pop();
                value = (double)values.Pop() * value;
                values.Push(value);
            }
            else if (opreator.Equals("**"))
            {
                value = (double)values.Pop();
                value = Math.Pow(values.Pop(), value);
                values.Push(value);
            }
            else if (opreator.Equals("/"))
            {
                value = (double)values.Pop();
                value = (double)values.Pop() / value;
                values.Push(value);
            }
            else if (opreator.Equals("="))
            {
                var firstValue = (int)GetLongValueFromString(values.Pop().ToString());
                var secondValue = (int)GetLongValueFromString(values.Pop().ToString());

                if (secondValue == firstValue)
                    tempBoolean = true;
            }
            else if (opreator.Equals(">="))
            {
                integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                if (GetLongValueFromString(values.Pop().ToString()) >= integerValue)
                    tempBoolean = true;
            }
            else if (opreator.Equals("<="))
            {
                integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                if (GetLongValueFromString(values.Pop().ToString()) <= integerValue)
                    tempBoolean = true;
            }
            else if (opreator.Equals("&"))
            {
                if (values.Count > 1)
                {
                    var byte2 = (int)GetLongValueFromString(values.Pop().ToString());
                    var byte1 = (int)GetLongValueFromString(values.Pop().ToString());
                    integerValue = (byte1 & byte2);
                    values.Push(integerValue);
                }
            }
            else if (opreator.Equals("|"))
            {
                if (values.Count > 1)
                {
                    var byte2 = (int)GetLongValueFromString(values.Pop().ToString());
                    var byte1 = (int)GetLongValueFromString(values.Pop().ToString());
                    integerValue = (byte1 | byte2);
                    values.Push(integerValue);
                }
            }
            else if (opreator.Equals("^"))
            {
                if (values.Count > 2)
                {
                    var byte2 = (int)GetLongValueFromString(values.Pop().ToString());
                    var byte1 = (int)GetLongValueFromString(values.Pop().ToString());
                    integerValue = (byte1 ^ byte2);
                    values.Push(integerValue);
                }
            }
            else if (opreator.Equals("~"))
            {
                integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                integerValue = ~integerValue;
                values.Push(integerValue);
            }
            else if (opreator.Equals(">"))
            {
                if (opreators.Count > 0)
                {
                    switch (opreators.Peek())
                    {
                        case ">":
                            opreators.Pop();
                            integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                            integerValue = ((int)GetLongValueFromString(values.Pop().ToString()) >> integerValue);
                            values.Push(integerValue);
                            break;

                        default:
                            integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                            if (GetLongValueFromString(values.Pop().ToString()) > integerValue)
                                tempBoolean = true;
                            break;
                    }
                }
                else
                {
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                    if (GetLongValueFromString(values.Pop().ToString()) > integerValue)
                        tempBoolean = true;
                }
            }
            else if (opreator.Equals("<"))
            {
                if (opreators.Count > 0)
                {
                    switch (opreators.Peek())
                    {
                        case "<":
                            opreators.Pop();
                            integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                            integerValue = ((int)GetLongValueFromString(values.Pop().ToString()) << integerValue);
                            values.Push(integerValue);
                            break;

                        default:
                            integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                            if (GetLongValueFromString(values.Pop().ToString()) < integerValue)
                                tempBoolean = true;
                            break;
                    }
                }
                else
                {
                    integerValue = (int)GetLongValueFromString(values.Pop().ToString());
                    if (GetLongValueFromString(values.Pop().ToString()) < integerValue)
                        tempBoolean = true;
                }
            }
            else if (opreator.Equals(":"))
            {
            }
            else if (opreator.Equals("ATAN"))
            {
                values.Push(Math.Atan(values.Pop()));
            }
            else if (opreator.Equals("COS"))
            {
                values.Push(Math.Cos(values.Pop()));
            }
            else if (opreator.Equals("SIN"))
            {
                values.Push(Math.Sin(values.Pop()));
            }
            else if (opreator.Equals("TAN"))
            {
                values.Push(Math.Tan(values.Pop()));
            }
            else if (opreator.Equals("ABS"))
            {
                values.Push(Math.Abs(values.Pop()));
            }
            else if (opreator.Equals("EXP"))
            {
                values.Push(Math.Exp(values.Pop()));
            }
            else if (opreator.Equals("LN"))
            {
                values.Push(Math.Log(values.Pop()));
            }
            else if (opreator.Equals("LG"))
            {
                values.Push(Math.Log10(values.Pop()));
            }
            else if (opreator.Equals("SQRT"))
            {
                values.Push(Math.Sqrt(values.Pop()));
            }
            else if (opreator.Equals("TRUNC"))
            {
                values.Push(Math.Truncate(values.Pop()));
            }
            else if (opreator.Equals("FLOOR"))
            {
                values.Push(Math.Floor(values.Pop()));
            }
            else if (opreator.Equals("CELL"))
            {
                values.Push(Math.Ceiling(values.Pop()));
            }
            else if (opreator.Equals("ROUND"))
            {
                values.Push(Math.Round(values.Pop()));
            }
            else if (opreator.Equals("ASIN"))
            {
                values.Push(Math.Asin(values.Pop()));
            }
            else if (opreator.Equals("ACOS"))
            {
                values.Push(Math.Acos(values.Pop()));
            }
            else if (opreator.Equals("TAN"))
            {
                values.Push(Math.Tan(values.Pop()));
            }

            return tempBoolean;
        }

        private long GetLongValueFromString(string value)
        {
            if (value.StartsWith("0x"))
            {
                value = value.Replace("0x", "");
                return long.Parse(value, System.Globalization.NumberStyles.HexNumber);
            }

            return long.Parse(value); ;
        }

        public async Task<long> GetValue()
        {
            return (Int64)await ExecuteFormulaFrom();
        }
    }
}