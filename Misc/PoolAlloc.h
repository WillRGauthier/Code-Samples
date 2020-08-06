// Defines a pool-based memory allocator, as well as helper structs
#pragma once
#include "DbgAssert.h"
#include <memory.h>

#if _WIN32 && _DEBUG
#define POOL_ALLOC_DEBUG
#elif DEBUG
#define POOL_ALLOC_DEBUG
#endif

// Defines a Pool Allocator
// Templated based on size of block and the number of blocks in the pool.
//
// To define your own pool to be used, it's recommended to typedef as such:
// typedef PoolAllocator<256, 1024> ComponentPool;
//
// IMPORTANT! StartUp must always be called before starting to use this,
// and ShutDown must be called once you're done with it. Or bad things happen.
template <size_t blockSize, unsigned int numBlocks>
class PoolAllocator
{
public:
	// The constructor dynamically allocates the pool on the heap.
	// 
	// mPool should be allocated to an array with numBlocks elements
	// Next, initialize the free list and all the mNext pointers in the list.
	// By default, you want index 0 of the mPool array to point to index 1, and so on.
	// Make sure you update mBlocksFree.
	//
	// #ifdef POOL_ALLOC_DEBUG, you should use memset on each mMemory element for each block,
	// writing the value 0xde over and over. Furthermore, each block should have its
	// mBoundary variable set to 0xdeadbeef.
	PoolAllocator();

	// The destructor should deallocate the mPool array, and set the number of free
	// blocks to 0.
	~PoolAllocator();

	// Allocate returns a pointer to usable memory within the pool.
	// 
	// It will DbgAssert size <= blockSize.
	// If the size is okay, remove the head PoolBlock from the free list,
	// and return the pointer to that PoolBlock's mMemory member
	// Make sure you update mBlocksFree.
	//
	// If there are no blocks available, it should trigger a DbgAssert and return nullptr.
	void* Allocate(size_t size);

	// Free should first cast the given pointer to a PoolBlock pointer, and then add
	// the block back to the front of the free list.
	//
	// Make sure you update iBlocksFree.
	//
	// #ifdef POOL_ALLOC_DEBUG, DbgAssert that boundary still == 0xdeadbeef (if not, the bounds were overwritten)
	// Also memset the blocks' mMemory member back to 0xde.
	//
	// Note that it's not straightforward to verify that the pointer actually belongs in the
	// pool, so don't call this on random pointers!
	void Free(void* ptr);

	// Returns the number of blocks free in the pool
	unsigned int GetNumBlocksFree() { return mBlocksFree; }

protected:
	// PoolBlock is a structure that we use as the building block for the pool-based allocator.
	// Notice that the size of mMemory is based on the PoolAllocator's block size
	struct PoolBlock
	{
		// This is the actual memory that the caller will be writing to.
		char mMemory[blockSize];

	#ifdef POOL_ALLOC_DEBUG
		// This boundary value is used to help find instances where memory is being written
		// beyond the mMemory array.
		unsigned int mDbgBoundary;
	#endif

		// Pointer to the next block in the free list.
		PoolBlock* mNext;

		PoolBlock()
		{
			mNext = nullptr;
		}
	};
	
	// This pointer will point to the array of all blocks
	PoolBlock* mPool;
	
	// This pointer represents the free list. Initially points to element at index 0,
	// but later updates like a forward linked list
	PoolBlock* mFreeList;

	// This keeps track of how many blocks are left in the pool
	unsigned int mBlocksFree;
};

// IMPLEMENTATIONS for PoolAllocator

// The constructor dynamically allocates the pool on the heap.
// 
// mPool should be allocated to an array with numBlocks elements
// Next, initialize the free list and all the mNext pointers in the list.
// By default, you want index 0 of the mPool array to point to index 1, and so on.
// Make sure you update mBlocksFree.
//
// #ifdef POOL_ALLOC_DEBUG, you should use memset on each mMemory element for each block,
// writing the value 0xde over and over. Furthermore, each block should have its
// mBoundary variable set to 0xdeadbeef.
template <size_t blockSize, unsigned int numBlocks>
PoolAllocator<blockSize, numBlocks>::PoolAllocator()
{
	mPool = new PoolBlock[numBlocks];
	mFreeList = mPool;
	for (int i = 0; i < numBlocks - 1; ++i)
		mPool[i].mNext = &mPool[i + 1];
	mBlocksFree = numBlocks;
	#ifdef POOL_ALLOC_DEBUG
	for (int i = 0; i < numBlocks; ++i)
	{
		memset(mPool[i].mMemory, 0xde, blockSize);
		mPool[i].mDbgBoundary = 0xdeadbeef;
	}
	#endif
}

// The destructor should deallocate the mPool array, and set the number of free
// blocks to 0.
template <size_t blockSize, unsigned int numBlocks>
PoolAllocator<blockSize, numBlocks>::~PoolAllocator()
{
	delete[] mPool;
	mBlocksFree = 0;
}

// Allocate returns a pointer to usable memory within the pool.
// 
// It will DbgAssert size <= blockSize.
// If the size is okay, remove the head PoolBlock from the free list,
// and return the pointer to that PoolBlock's mMemory member
// Make sure you update mBlocksFree.
//
// If there are no blocks available, it should trigger a DbgAssert and return nullptr.
template <size_t blockSize, unsigned int numBlocks>
void* PoolAllocator<blockSize, numBlocks>::Allocate(size_t size)
{
	DbgAssert(size <= blockSize, "Allocation request is bigger than block.");
	DbgAssert(mFreeList != nullptr, "No memory blocks available.");
	if (size > blockSize || mFreeList == nullptr)
		return nullptr;

	PoolBlock* temp = mFreeList;
	mFreeList = mFreeList->mNext;
	--mBlocksFree;
	return temp;
}

// Free should first cast the given pointer to a PoolBlock pointer, and then add
// the block back to the front of the free list.
//
// Make sure you update mBlocksFree.
//
// #ifdef POOL_ALLOC_DEBUG, DbgAssert that boundary still == 0xdeadbeef (if not, the bounds were overwritten)
// Also memset the blocks' mMemory member back to 0xde.
//
// Note that it's not straightforward to verify that the pointer actually belongs in the
// pool, so don't call this on random pointers!
template <size_t blockSize, unsigned int numBlocks>
void PoolAllocator<blockSize, numBlocks>::Free(void* ptr)
{
	PoolBlock* tempPoolBlock = (PoolBlock*)ptr;
	tempPoolBlock->mNext = mFreeList;
	mFreeList = tempPoolBlock;
	++mBlocksFree;
	#ifdef POOL_ALLOC_DEBUG
	DbgAssert(tempPoolBlock->mDbgBoundary == 0xdeadbeef, "Bounds were overwritten.");
	memset(tempPoolBlock, 0xde, blockSize);
	#endif
}
