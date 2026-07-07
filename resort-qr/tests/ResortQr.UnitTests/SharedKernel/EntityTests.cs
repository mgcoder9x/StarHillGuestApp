using System;
using System.Collections.Generic;
using ResortQr.SharedKernel.Entities;
using Xunit;

namespace ResortQr.UnitTests.SharedKernel;

public sealed class EntityTests
{
    private sealed class SampleEntity : Entity
    {
        public SampleEntity()
        {
        }

        public SampleEntity(Guid id) : base(id)
        {
        }
    }

    private sealed class OtherEntity : Entity;

    [Fact]
    public void New_entity_should_have_nonempty_uuid_v7_id()
    {
        var e = new SampleEntity();

        Assert.NotEqual(Guid.Empty, e.Id);
        Assert.Equal(7, e.Id.Version); // UUIDv7
    }

    [Fact]
    public void Setting_empty_guid_via_initializer_should_throw()
    {
        Assert.Throws<ArgumentException>(() => new SampleEntity { Id = Guid.Empty });
    }

    [Fact]
    public void Two_new_entities_should_have_distinct_ids()
    {
        var a = new SampleEntity();
        var b = new SampleEntity();

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void Entities_of_same_type_and_id_should_be_equal()
    {
        var id = Guid.CreateVersion7();
        var a = new SampleEntity(id);
        var b = new SampleEntity(id);

        Assert.Equal(a, b);
        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Entities_of_different_type_same_id_should_not_be_equal()
    {
        var id = Guid.CreateVersion7();
        Entity a = new SampleEntity(id);
        Entity b = new OtherEntity { Id = id };

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Uuid_v7_timestamp_prefix_should_be_non_decreasing_in_creation_order()
    {
        // UUIDv7: 48 bit đầu (big-endian) = Unix ms timestamp. Đây mới là thứ tự thời gian THẬT.
        // Lưu ý (TK-002): Guid.CompareTo/sort KHÔNG phản ánh thứ tự này; thứ tự index trên
        // PostgreSQL là chuyện tầng DB (benchmark riêng), không kiểm ở unit test.
        static long Timestamp(Guid id)
        {
            var b = id.ToByteArray(bigEndian: true);
            return ((long)b[0] << 40) | ((long)b[1] << 32) | ((long)b[2] << 24)
                 | ((long)b[3] << 16) | ((long)b[4] << 8) | b[5];
        }

        var timestamps = new List<long>();
        for (var i = 0; i < 200; i++)
        {
            timestamps.Add(Timestamp(new SampleEntity().Id));
        }

        for (var i = 1; i < timestamps.Count; i++)
        {
            Assert.True(
                timestamps[i] >= timestamps[i - 1],
                "Timestamp prefix của UUIDv7 phải không giảm theo thứ tự sinh.");
        }
    }
}
