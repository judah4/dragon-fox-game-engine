using Foxis.Library;
using Foxis.Library.Freelists;

namespace Foxis.Library.Tests
{
    [TestClass]
    public class FreeListTests
    {
        [TestMethod]
        public void Freelist_should_create_and_destroy()
        {
            FreeList list = new FreeList(100000);
            list.Destroy();
        }

        [TestMethod]
        public void freelist_should_allocate_one_and_free_one()
        {
            const uint totalSize = 100000;
            FreeList list = new FreeList(totalSize);

            // Allocate some space.
            var result = list.AllocateBlock(64);
            // Verify that result is true, offset should be set to 0.
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0U, result.Value, "Expecting offset to be 0.");

            // Verify that the correct amount of space is free.
            var free_space = list.GetFreeSpace();
            Assert.AreEqual(totalSize - 64, free_space);

            // Now free the block.
            var freeResult = list.FreeBlock(64, result.Value);
            // Verify that result is true
            Assert.IsTrue(freeResult.Success);

            // Verify the entire block is free.
            free_space = list.GetFreeSpace();
            Assert.AreEqual(totalSize, free_space);

            // Destroy and verify that the memory was unassigned.
            list.Destroy();

        }

        [TestMethod]
        public void freelist_should_allocate_one_and_free_multi()
        {
            const uint totalSize = 100000;
            FreeList list = new FreeList(totalSize);

            // Allocate some space.
            var result = list.AllocateBlock(64);
            // Verify that result is true, offset should be set to 0.
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0U, result.Value, "Expecting offset to be 0.");
            var offset1 = result.Value;

            // Allocate some more space.
            result = list.AllocateBlock(64);
            // Verify that result is true, offset should be set to the offset+size of the previous allocation.
            Assert.IsTrue(result.Success);
            Assert.AreEqual(64U, result.Value, "Expecting offset to not be 0.");
            var offset2 = result.Value;

            // Allocate one more space.
            result = list.AllocateBlock(64);
            // Verify that result is true, offset should be set to the offset+size of the previous allocation.
            Assert.IsTrue(result.Success);
            Assert.AreEqual(128U, result.Value, "Expecting offset to not be 0.");
            var offset3 = result.Value;

            // Verify that the correct amount of space is free.
            var free_space = list.GetFreeSpace();
            Assert.AreEqual(totalSize - 192, free_space);

            // Now free the middle block.
            var freeResult = list.FreeBlock(64, offset2);
            // Verify that result is true
            Assert.IsTrue(freeResult.Success);

            // Verify the correct amount is free.
            free_space = list.GetFreeSpace();
            Assert.AreEqual(totalSize - 128, free_space);

            // Allocate some more space, this should fill the middle block back in.
            result = list.AllocateBlock(64);
            // Verify that result is true, offset should be set to the offset+size of the previous allocation.
            Assert.IsTrue(result.Success);
            Assert.AreEqual(offset2, result.Value, "Expecting offset to be equal to previous offset 2");
            var offset4 = result.Value;

            // Verify that the correct amount of space is free.
            free_space = list.GetFreeSpace();
            Assert.AreEqual(totalSize - 192, free_space);

            // Free the first block and verify space.
            freeResult = list.FreeBlock(64, offset1);
            Assert.IsTrue(freeResult.Success);
            free_space = list.GetFreeSpace();
            Assert.AreEqual(totalSize - 128, free_space);

            // Free the last block and verify space.
            freeResult = list.FreeBlock(64, offset3);
            Assert.IsTrue(freeResult.Success);
            free_space = list.GetFreeSpace();
            Assert.AreEqual(totalSize - 64, free_space);

            // Free the middle block and verify space.
            freeResult = list.FreeBlock(64, offset4);
            Assert.IsTrue(freeResult.Success);
            free_space = list.GetFreeSpace();
            Assert.AreEqual(totalSize, free_space);

            // Destroy and verify that the memory was unassigned.
            list.Destroy();

        }

        [TestMethod]
        public void freelist_should_allocate_one_and_free_multi_varying_sizes()
        {
            const uint totalSize = 100000;
            FreeList list = new FreeList(totalSize);

            // Allocate some space.
            var result = list.AllocateBlock(64);
            // Verify that result is true, offset should be set to 0.
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0U, result.Value, "Expecting offset to be 0.");
            var offset1 = result.Value;

            // Allocate some more space.
            result = list.AllocateBlock(32);
            // Verify that result is true, offset should be set to 0.
            Assert.IsTrue(result.Success);
            Assert.AreEqual(64U, result.Value, "Expecting offset to not be 0.");
            var offset2 = result.Value;

            // Allocate one more space.
            result = list.AllocateBlock(64);
            // Verify that result is true, offset should be set to 0.
            Assert.IsTrue(result.Success);
            Assert.AreEqual(96U, result.Value, "Expecting offset to not be 0.");
            var offset3 = result.Value;

            // Verify that the correct amount of space is free.
            var free_space = list.GetFreeSpace();
            Assert.AreEqual(totalSize - 160, free_space);

            // Now free the middle block.
            var freeResult = list.FreeBlock(32, offset2);
            // Verify that result is true
            Assert.IsTrue(freeResult.Success);

            // Verify the correct amount is free.
            free_space = list.GetFreeSpace();
            Assert.AreEqual(totalSize - 128, free_space);

            // Allocate some more space, this time larger than the old middle block. This should have a new offset
            // at the end of the list.
            result = list.AllocateBlock(64);
            // Verify that result is true, offset should be set to 0.
            Assert.IsTrue(result.Success);
            Assert.AreEqual(160U, result.Value, "Expecting offset to be at the end of the allocations.");
            var offset4 = result.Value;


            // Verify that the correct amount of space is free.
            free_space = list.GetFreeSpace();
            Assert.AreEqual(totalSize - 192, free_space);

            // Free the first block and verify space.
            freeResult = list.FreeBlock(64, offset1);
            Assert.IsTrue(freeResult.Success);
            free_space = list.GetFreeSpace();
            Assert.AreEqual(totalSize - 128, free_space);

            // Free the last block and verify space.
            freeResult = list.FreeBlock(64, offset3);
            Assert.IsTrue(freeResult.Success);
            free_space = list.GetFreeSpace();
            Assert.AreEqual(totalSize - 64, free_space);

            // Free the middle (now end) block and verify space.
            freeResult = list.FreeBlock(64, offset4);
            Assert.IsTrue(freeResult.Success);
            free_space = list.GetFreeSpace();
            Assert.AreEqual(totalSize, free_space);

            // Destroy and verify that the memory was unassigned.
            list.Destroy();

        }

        [TestMethod]
        public void freelist_should_allocate_to_full_and_fail_to_allocate_more()
        {
            const uint totalSize = 100000;
            FreeList list = new FreeList(totalSize);

            // Allocate all space.
            var result = list.AllocateBlock(totalSize);
            // Verify that result is true, offset should be set to 0.
            Assert.IsTrue(result.Success);
            Assert.AreEqual(0U, result.Value, "Expecting offset to be 0.");

            // Verify that the correct amount of space is free.
            var free_space = list.GetFreeSpace();
            Assert.AreEqual(0U, free_space);

            // Now try allocating some more
            result = list.AllocateBlock(64);
            // Verify that result is false
            Assert.IsFalse(result.Success);

            // Verify that the correct amount of space is free.
            free_space = list.GetFreeSpace();
            Assert.AreEqual(0U, free_space);

            // Destroy and verify that the memory was unassigned.
            list.Destroy();

        }
    }
}