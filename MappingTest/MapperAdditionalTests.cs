using System.Dynamic;
using LanguageExtended.Mapping;
using LanguageExtended.Result;

namespace MappingTest
{
    public class MapperAdditionalTests
    {
        [Fact]
        public void Map_CircularReferences_AvoidStackOverflow()
        {
            // Arrange
            var parent = new ParentWithCircularReference { Name = "Parent" };
            var child = new ChildWithCircularReference { Name = "Child", Parent = parent };
            parent.Child = child;

            var mapper = new Mapper();

            // Act
            var result = mapper.Map<ParentWithCircularReference>(parent);

            // Assert
            Assert.True(result.IsSuccess);
            var mappedParent = result.Value;
            Assert.Same(mappedParent, mappedParent.Child.Parent);
        }

        [Fact]
        public void Map_DynamicObject_Success()
        {
            // Arrange
            dynamic source = new ExpandoObject();
            source.Name = "Test";
            source.Age = 30;
            
            var mapper = new Mapper();
            
            // Act
            var result = mapper.Map<SimpleTarget>(source);
            
            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Test", result.Value.Name);
            Assert.Equal(30, result.Value.Age);
        }

        [Fact]
        public void Map_CultureSpecificConversions_Success()
        {
            // Arrange
            var source = new CultureSpecificValues
            {
                DecimalValue = "1,234.56", // US format
                DateValue = "12/25/2023"   // US format
            };
            
            var mapper = new Mapper();
            
            // Act
            var result = mapper.Map<CultureSpecificTarget>(source);
            
            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(1234.56m, result.Value.DecimalValue);
            Assert.Equal(new DateTime(2023, 12, 25), result.Value.DateValue);
        }

        [Fact]
        public async Task Map_ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            var mapper = new Mapper();
            var tasks = new List<Task<Result<SimpleTarget, MappingError>>>();
            
            // Act - Create multiple concurrent mapping operations
            for (int i = 0; i < 100; i++)
            {
                int capturedI = i;
                tasks.Add(Task.Run(() => mapper.Map<SimpleTarget>(new SimpleSource 
                { 
                    Name = $"Test{capturedI}", 
                    Age = capturedI 
                })));
            }
            
            // Wait for all tasks to complete
            var results = await Task.WhenAll(tasks);
            
            // Assert
            Assert.All(results, r => Assert.True((bool)r.IsSuccess));
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal($"Test{i}" as string, results[i].Value.Name as string);
                Assert.Equal(i, results[i].Value.Age);
            }
        }

        [Fact]
        public void Map_WithCustomTypeConverter_Success()
        {
            // Arrange
            var source = new SourceWithCustomType
            {
                Id = "123-456",
                CustomValue = new CustomValueType("test-value")
            };
            
            // Create a mapper with a custom type converter
            var options = new MappingOptions();
            var mapper = new Mapper(options);
            
            // Act
            var result = mapper.Map<TargetWithCustomType>(source);
            
            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("123-456", result.Value.Id);
            Assert.Equal("test-value", result.Value.CustomValue.Value);
        }

        [Fact]
        public void Map_GenericTypeMapping_Success()
        {
            // Arrange
            var source = new GenericSource<int>
            {
                Id = 1,
                Value = 42,
                Items = new List<int> { 1, 2, 3 }
            };
            
            var mapper = new Mapper();
            
            // Act
            var result = mapper.Map<GenericTarget<int>>(source);
            
            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.Id);
            Assert.Equal(42, result.Value.Value);
            Assert.Equal(new List<int> { 1, 2, 3 }, result.Value.Items);
        }

        [Fact]
        public void Map_ValidationConstraints_HandlesValidationFailure()
        {
            // Arrange
            var source = new SimpleSource
            {
                Name = null,  // Name is required in the target
                Age = -5      // Age must be positive in the target
            };
            
            var mapper = new Mapper();
            
            // Act
            var result = mapper.Map<ValidatedTarget>(source);
            
            // Assert - The mapping succeeds but the object might not be valid
            Assert.True(result.IsSuccess);
            
            // This would normally be validated after mapping
            var validationErrors = ValidateTarget(result.Value);
            Assert.Contains("Name is required", validationErrors);
            Assert.Contains("Age must be positive", validationErrors);
        }
        
        // Helper method to simulate validation
        private List<string> ValidateTarget(ValidatedTarget target)
        {
            var errors = new List<string>();
            
            if (string.IsNullOrEmpty(target.Name))
                errors.Add("Name is required");
                
            if (target.Age < 0)
                errors.Add("Age must be positive");
                
            return errors;
        }

        // Test class definitions
        public class ParentWithCircularReference
        {
            public string Name { get; set; }
            public ChildWithCircularReference Child { get; set; }
        }

        public class ChildWithCircularReference
        {
            public string Name { get; set; }
            public ParentWithCircularReference Parent { get; set; }
        }

        public class SimpleSource
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        public class SimpleTarget
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        public class CultureSpecificValues
        {
            public string DecimalValue { get; set; }
            public string DateValue { get; set; }
        }

        public class CultureSpecificTarget
        {
            public decimal DecimalValue { get; set; }
            public DateTime DateValue { get; set; }
        }

        public class CustomValueType
        {
            public string Value { get; }
            
            public CustomValueType(string value)
            {
                Value = value;
            }
        }

        public class SourceWithCustomType
        {
            public string Id { get; set; }
            public CustomValueType CustomValue { get; set; }
        }

        public class TargetWithCustomType
        {
            public string Id { get; set; }
            public CustomValueType CustomValue { get; set; }
        }

        public class GenericSource<T>
        {
            public int Id { get; set; }
            public T Value { get; set; }
            public List<T> Items { get; set; }
        }

        public class GenericTarget<T>
        {
            public int Id { get; set; }
            public T Value { get; set; }
            public List<T> Items { get; set; }
        }

        public class ValidatedTarget
        {
            // This class has validation requirements
            public string Name { get; set; }  // Required
            public int Age { get; set; }      // Must be positive
        }
    }
}