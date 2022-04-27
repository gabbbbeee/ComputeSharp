﻿using ComputeSharp;
using ComputeSharp.Interop;
using ComputeSharp.Tests.Misc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ComputeSharp.Tests
{
    [TestClass]
    [TestCategory("ShaderCompiler")]
    public partial class ShaderCompilerTests
    {
        [TestMethod]
        public void ReservedKeywords()
        {
            _ = ReflectionServices.GetShaderInfo<ReservedKeywordsShader>();
        }

        [AutoConstructor]
        public readonly partial struct ReservedKeywordsShader : IComputeShader
        {
            public readonly ReadWriteBuffer<float> row_major;
            public readonly float dword;
            public readonly float float2;
            public readonly int int2x2;

            public void Execute()
            {
                float exp = Hlsl.Exp(dword * row_major[ThreadIds.X]);
                float log = Hlsl.Log(1 + exp);

                row_major[ThreadIds.X] = log / dword + float2 + int2x2;
            }
        }

        [TestMethod]
        public void ReservedKeywordsPrecompiled()
        {
            _ = ReflectionServices.GetShaderInfo<ReservedKeywordsPrecompiledShader>();
        }

        [AutoConstructor]
        [EmbeddedBytecode(DispatchAxis.X)]
        public readonly partial struct ReservedKeywordsPrecompiledShader : IComputeShader
        {
            public readonly ReadWriteBuffer<float> row_major;
            public readonly float dword;
            public readonly float float2;
            public readonly int int2x2;
            private readonly float sin;
            private readonly float cos;
            private readonly float scale;
            private readonly float intensity;

            public void Execute()
            {
                float exp = Hlsl.Exp(dword * row_major[ThreadIds.X]);
                float log = Hlsl.Log(1 + exp);

                float s1 = this.cos * exp * this.sin * log;
                float t1 = -this.sin * exp * this.cos * log;

                float s2 = s1 + this.intensity + Hlsl.Tan(s1 * this.scale);
                float t2 = t1 + this.intensity + Hlsl.Tan(t1 * this.scale);

                float u2 = this.cos * s2 - this.sin * t2;
                float v2 = this.sin * s2 - this.cos * t2;

                row_major[ThreadIds.X] = log / dword + float2 + int2x2 + u2 + v2;
            }
        }

        [TestMethod]
        public void SpecialTypeAsReturnType()
        {
            _ = ReflectionServices.GetShaderInfo<SpecialTypeAsReturnTypeShader>();
        }

        [AutoConstructor]
        public readonly partial struct SpecialTypeAsReturnTypeShader : IComputeShader
        {
            public readonly ReadWriteBuffer<float2> buffer;

            float2 Foo(float x) => x;

            public void Execute()
            {
                static float3 Bar(float x) => x;

                buffer[ThreadIds.X] = Foo(ThreadIds.X) + Bar(ThreadIds.X).XY;
            }
        }

        [TestMethod]
        public void LocalFunctionInExternalMethods()
        {
            _ = ReflectionServices.GetShaderInfo<LocalFunctionInExternalMethodsShader>();
        }

        [AutoConstructor]
        public readonly partial struct LocalFunctionInExternalMethodsShader : IComputeShader
        {
            public readonly ReadWriteBuffer<float2> buffer;

            float2 Foo(float x)
            {
                static float2 Baz(float y) => y;

                return Baz(x);
            }

            public void Execute()
            {
                buffer[ThreadIds.X] = Foo(ThreadIds.X);
            }
        }

        [TestMethod]
        public void CapturedNestedStructType()
        {
            _ = ReflectionServices.GetShaderInfo<CapturedNestedStructTypeShader>();
        }

        [AutoConstructor]
        public partial struct CustomStructType
        {
            public float2 a;
            public int b;
        }

        [AutoConstructor]
        public readonly partial struct CapturedNestedStructTypeShader : IComputeShader
        {
            public readonly ReadWriteBuffer<float> buffer;
            public readonly CustomStructType foo;

            /// <inheritdoc/>
            public void Execute()
            {
                buffer[ThreadIds.X] *= foo.a.X + foo.b;
            }
        }

        [TestMethod]
        public void ExternalStructType_Ok()
        {
            _ = ReflectionServices.GetShaderInfo<ExternalStructTypeShader>();
        }

        [AutoConstructor]
        public readonly partial struct ExternalStructTypeShader : IComputeShader
        {
            public readonly ReadWriteBuffer<float> buffer;

            /// <inheritdoc/>
            public void Execute()
            {
                float value = buffer[ThreadIds.X];
                ExternalStructType type = ExternalStructType.New((int)value, Hlsl.Abs(value));

                buffer[ThreadIds.X] = ExternalStructType.Sum(type);
            }
        }

        [TestMethod]
        public void OutOfOrderMethods()
        {
            _ = ReflectionServices.GetShaderInfo<OutOfOrderMethodsShader>();
        }

        [AutoConstructor]
        public readonly partial struct OutOfOrderMethodsShader : IComputeShader
        {
            public readonly ReadWriteBuffer<float> buffer;

            static int A() => B();

            static int B() => 1 + C();

            static int C() => 1;

            int D() => A() + E() + F();

            int E() => 1;

            static int F() => 1;

            /// <inheritdoc/>
            public void Execute()
            {
                float value = buffer[ThreadIds.X];
                ExternalStructType type = ExternalStructType.New((int)value, Hlsl.Abs(value));

                buffer[ThreadIds.X] = ExternalStructType.Sum(type);
            }
        }

        [TestMethod]
        public void PixelShader()
        {
            ShaderInfo info = ReflectionServices.GetShaderInfo<StatelessPixelShader, float4>();

            Assert.AreEqual(info.TextureStoreInstructionCount, 1u);
            Assert.AreEqual(info.BoundResourceCount, 2u);
        }

        public readonly partial struct StatelessPixelShader : IPixelShader<float4>
        {
            /// <inheritdoc/>
            public float4 Execute()
            {
                return new(1, 1, 1, 1);
            }
        }

        [AutoConstructor]
        public readonly partial struct LoopWithVarCounterShader : IComputeShader
        {
            public readonly ReadWriteBuffer<float> buffer;

            /// <inheritdoc/>
            public void Execute()
            {
                for (var i = 0; i < 10; i++)
                {
                    buffer[ThreadIds.X * 10 + i] = i;
                }
            }
        }

        [TestMethod]
        public void LoopWithVarCounter()
        {
            _ = ReflectionServices.GetShaderInfo<LoopWithVarCounterShader>();
        }

        [TestMethod]
        public void DoublePrecisionSupport()
        {
            ShaderInfo info = ReflectionServices.GetShaderInfo<DoublePrecisionSupportShader>();

            Assert.IsTrue(info.RequiresDoublePrecisionSupport);
        }

        [AutoConstructor]
        public readonly partial struct DoublePrecisionSupportShader : IComputeShader
        {
            public readonly ReadWriteBuffer<double> buffer;
            public readonly double factor;

            /// <inheritdoc/>
            public void Execute()
            {
                buffer[ThreadIds.X] *= factor + 3.14;
            }
        }

        [TestMethod]
        public void FieldAccessWithThisExpression()
        {
            _ = ReflectionServices.GetShaderInfo<FieldAccessWithThisExpressionShader>();
        }

        [AutoConstructor]
        internal readonly partial struct FieldAccessWithThisExpressionShader : IComputeShader
        {
            public readonly ReadWriteBuffer<float> buffer;
            public readonly float number;

            /// <inheritdoc/>
            public void Execute()
            {
                this.buffer[ThreadIds.X] *= this.number;
            }
        }
    }
}

namespace ExternalNamespace
{
    [TestClass]
    [TestCategory("ShaderCompiler")]
    public partial class ShaderCompilerTestsInExternalNamespace
    {
        [AutoConstructor]
        public readonly partial struct UserDefinedTypeShader : IComputeShader
        {
            public readonly ReadWriteBuffer<float> buffer;

            /// <inheritdoc/>
            public void Execute()
            {
                for (var i = 0; i < 10; i++)
                {
                    buffer[ThreadIds.X * 10 + i] = i;
                }
            }
        }

        [TestMethod]
        public void UserDefinedType()
        {
            _ = ReflectionServices.GetShaderInfo<UserDefinedTypeShader>();
        }
    }
}
