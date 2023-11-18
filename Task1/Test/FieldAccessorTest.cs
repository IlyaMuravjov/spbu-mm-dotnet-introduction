using FieldAccessor;

namespace FieldAccessorTest
{
    [TestClass]
    public abstract class AbstractFieldAccessorFactoryTest
    {
        class Group(int number)
        {
            private int number = number;
        }

        readonly struct Student(Group group)
        {
            private readonly Group group = group;
        }

        protected abstract IFieldAccessorFactory CreateFieldAccessorFactory();

        [TestMethod]
        public void WhenGivenPathWithNoDotsFieldAccessorShouldAccessTopLevelField()
        {
            Group group = new(471);
            Student student = new(group);
            Func<Student, Group?> fieldAccessor = CreateFieldAccessorFactory().GetFieldAccessor<Student, Group>("group");
            Group? actual = fieldAccessor(student);
            Group expected = group;
            Assert.AreSame(expected, actual);
        }

        [TestMethod]
        public void WhenGivenDotSeparatedPathFieldAccessorShouldAccessNestedField()
        {
            Student student = new(new Group(471));
            Func<Student, int> fieldAccessor = CreateFieldAccessorFactory().GetFieldAccessor<Student, int>("group.number");
            int actual = fieldAccessor(student);
            int expected = 471;
            Assert.AreEqual(expected, actual);
        }
    }

    [TestClass]
    public class ExpressionBasedFieldAccessorFactoryTest : AbstractFieldAccessorFactoryTest
    {
        protected override IFieldAccessorFactory CreateFieldAccessorFactory() => new ExpressionBasedFieldAccessorFactory();
    }

    [TestClass]
    public class ReflectionBasedFieldAccessorFactoryTest : AbstractFieldAccessorFactoryTest
    {
        protected override IFieldAccessorFactory CreateFieldAccessorFactory() => new ReflectionBasedFieldAccessorFactory();
    }
}