using System;

namespace SampleApp.Math
{
    /// <summary>
    /// Provides basic mathematical operations.
    /// </summary>
    public class Calculator
    {
        /// <summary>
        /// Adds two numbers together.
        /// </summary>
        public double Add(double a, double b) => a + b;

        /// <summary>
        /// Subtracts the second number from the first.
        /// </summary>
        public double Subtract(double a, double b) => a - b;

        /// <summary>
        /// Multiplies two numbers.
        /// </summary>
        public double Multiply(double a, double b) => a * b;

        /// <summary>
        /// Divides the first number by the second.
        /// </summary>
        /// <exception cref="DivideByZeroException">Thrown when b is zero.</exception>
        public double Divide(double a, double b)
        {
            if (b == 0)
                throw new DivideByZeroException("Cannot divide by zero");
            return a / b;
        }

        /// <summary>
        /// Calculates the power of a number.
        /// </summary>
        public double Power(double baseNum, double exponent) => System.Math.Pow(baseNum, exponent);

        /// <summary>
        /// Calculates the square root of a number.
        /// </summary>
        public double SquareRoot(double number)
        {
            if (number < 0)
                throw new ArgumentException("Cannot calculate square root of negative number");
            return System.Math.Sqrt(number);
        }
    }

    /// <summary>
    /// Provides advanced mathematical operations.
    /// </summary>
    public static class AdvancedMath
    {
        /// <summary>
        /// Calculates the factorial of a number.
        /// </summary>
        public static long Factorial(int n)
        {
            if (n < 0)
                throw new ArgumentException("Factorial is not defined for negative numbers");
            if (n == 0 || n == 1)
                return 1;
            
            long result = 1;
            for (int i = 2; i <= n; i++)
            {
                result *= i;
            }
            return result;
        }

        /// <summary>
        /// Determines if a number is prime.
        /// </summary>
        public static bool IsPrime(int number)
        {
            if (number <= 1)
                return false;
            if (number == 2)
                return true;
            if (number % 2 == 0)
                return false;

            var boundary = (int)System.Math.Floor(System.Math.Sqrt(number));
            for (int i = 3; i <= boundary; i += 2)
            {
                if (number % i == 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Calculates the Fibonacci number at position n.
        /// </summary>
        public static long Fibonacci(int n)
        {
            if (n < 0)
                throw new ArgumentException("Position must be non-negative");
            if (n <= 1)
                return n;

            long prev = 0, curr = 1;
            for (int i = 2; i <= n; i++)
            {
                long temp = prev + curr;
                prev = curr;
                curr = temp;
            }
            return curr;
        }
    }
}