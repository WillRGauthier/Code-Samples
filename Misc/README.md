# Misc Code Explanations

## [Assembly Clock Counter](lab6.asm)
The goal of my electrical engineering lab was to gain experience working with interrupts by writing a fake 21-day clock in assembly that had to reset once it hit 20:23:59:59. I pushed myself to go beyond the expectation of simply incrementing separate variables for each time unit to instead use real time. I figured out how to query the system clock for total time in milliseconds and learned how to do 64 bit addition, subtraction, and division to convert between units.

## [Pool Allocator](PoolAlloc.h) & [SIMD Math](SimdMath.h)
For the first lab of my game engines course I implemented a pooled memory allocator and vector and matrix operations via the Streaming SIMD (single instruction, multiple data) Extensions (SSE) in C++.

## [Vector](Vector.h), [Matrix](Matrix.h), and [Quaternion](Quaternion.h) Math Library
As an initial foray into writing a C++ game engine, I developed this templated math library enabling quaternions, arbitrarily sized matrices, and arbitrarily sized vectors of any arithmetic type, with template specialization, static constants, and using aliases for common cases. Per Scott Meyers' Effective C++ Item 44, the matrices and vectors have base classes with the size templated to avoid code-bloated binaries.