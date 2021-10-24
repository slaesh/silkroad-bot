using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace MVVM
{
    /// <summary>
    /// This should be the base class for all view models.
    /// Every class with bounded properties should derive from this class,
    /// because here is the implementation of the "INotifyPropertyChanged"-Interface located.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        #region < INotifyPropertyChanged > Members

        /// <summary>
        /// Is connected to a method which handle changes to a property (located in the WPF Data Binding Engine)
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raise the [PropertyChanged] event
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        private Dictionary<string, object> propertyValueStorage;

        #region Constructor

        public ViewModelBase()
        {
            this.propertyValueStorage = new Dictionary<string, object>();
        }

        #endregion

        /// <summary>
        /// Set the value of the property and raise the [PropertyChanged] event
        /// (only if the saved value and the new value are not equal)
        /// </summary>
        /// <typeparam name="T">The property type</typeparam>
        /// <param name="property">The property as a lambda expression</param>
        /// <param name="value">The new value of the property</param>
        protected void SetValue<T>(Expression<Func<T>> property, T value)
        {
            LambdaExpression lambdaExpression = property as LambdaExpression;

            if (lambdaExpression == null)
            {
                throw new ArgumentException("Invalid lambda expression", "Lambda expression return value can't be null");
            }

            string propertyName = this.getPropertyName(lambdaExpression);

            T storedValue = this.getValue<T>(propertyName);

            if (!object.Equals(storedValue, value))
            {
                this.propertyValueStorage[propertyName] = value;
                this.OnPropertyChanged(propertyName);
            }
        }

        /// <summary>
        /// Get the value of the property
        /// </summary>
        /// <typeparam name="T">The property type</typeparam>
        /// <param name="property">The property as a lambda expression</param>
        /// <returns>The value of the given property (or the default value)</returns>
        protected T GetValue<T>(Expression<Func<T>> property)
        {
            LambdaExpression lambdaExpression = property as LambdaExpression;

            if (lambdaExpression == null)
            {
                throw new ArgumentException("Invalid lambda expression", "Lambda expression return value can't be null");
            }

            string propertyName = this.getPropertyName(lambdaExpression);

            return getValue<T>(propertyName);
        }

        /// <summary>
        /// Try to get the value from the internal dictionary of the given property name
        /// </summary>
        /// <typeparam name="T">The property type</typeparam>
        /// <param name="propertyName">The name of the property</param>
        /// <returns>Retrieve the value from the internal dictionary</returns>
        private T getValue<T>(string propertyName)
        {
            object value;

            if (propertyValueStorage.TryGetValue(propertyName, out value))
            {
                return (T)value;
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>
        /// Extract the property name from a lambda expression
        /// </summary>
        /// <param name="lambdaExpression">The lambda expression with the property</param>
        /// <returns>The extracted property name</returns>
        private string getPropertyName(LambdaExpression lambdaExpression)
        {
            MemberExpression memberExpression;

            if (lambdaExpression.Body is UnaryExpression)
            {
                var unaryExpression = lambdaExpression.Body as UnaryExpression;
                memberExpression = unaryExpression.Operand as MemberExpression;
            }
            else
            {
                memberExpression = lambdaExpression.Body as MemberExpression;
            }

            return memberExpression.Member.Name;
        }

        //+-------------------------------------------------------------------------------------
        //+ The methods below are for the standard approach :
        //+ =================================================
        //+ public string property;
        //+ public string Property
        //+ {
        //+   get { return property; }
        //+   set
        //+   {
        //+      if (property != value)
        //+      {
        //+         property = value;
        //+         RaisePropertyChangedEvent( () => Property );
        //+      }
        //+   }
        //+ }
        //+-------------------------------------------------------------------------------------

        /// <summary>
        /// "Raise" the PropertyChanged-Event (parameterless => similar to the dotNet Framework 4.5 version)
        /// C# 5.0 : private void NotifyPropertyChanged([CallerMemberName] String propertyName = "") 
        /// </summary>
        protected void RaisePropertyChangedEvent([CallerMemberName] String propertyName = "")
        {
#if false
            // Get the call stack
            StackTrace stackTrace = new StackTrace();

            // Get the calling method name
            string callingMethodName = stackTrace.GetFrame(1).GetMethod().Name;

            // Check if the callingMethodName contains an underscore like in "set_SomeProperty"
            if (callingMethodName.Contains("_"))
            {
                // Extract the property name
                string propertyName = callingMethodName.Split('_')[1];
#endif
            if (this.PropertyChanged != null && propertyName != String.Empty)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
#if false
            }
#endif
        }

        /// <summary>
        /// "Raise" the PropertyChanged-Event through a lambda expression
        /// </summary>
        /// <param name="lambdaExpression"></param>
        protected void RaisePropertyChangedEvent(Expression<Func<object>> lambdaExpression)
        {
            // The changed property is not identified through a string but rather through the property itself
            if (this.PropertyChanged != null)
            {
                // Extract the body of the lambda expression
                var lambdaBody = lambdaExpression.Body as MemberExpression;

                if (lambdaBody == null)
                {
                    // If the Property is a primitive data type (i.e. bool) the body of the lambda expression
                    // have to be converted to an "UnaryExpression" to get the desired "MemberExpression"
                    var unaryExpression = (lambdaExpression.Body as UnaryExpression);
                    lambdaBody = unaryExpression.Operand as MemberExpression;
                }

                // "Raise" the PropertyChanged-Event with the "real name" of the Property
                this.PropertyChanged(this, new PropertyChangedEventArgs(lambdaBody.Member.Name));
            }
        }

#if false
        /// <summary>
        /// "Raise" the PropertyChanged-Event (string based with parameter check)
        /// => It is recommended to use the lambda version or the parameterless version
        /// </summary>
        /// <param name="propertyName"></param>
        protected void RaisePropertyChangedEvent (string propertyName)
        {
            // This is an improved "string based" version to "raise" the PropertyChanged-Event
            // Bevor raising the PropertyChanged Event, the property name is being evaluated !
            this.checkPropertyName(propertyName);

            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
#endif

        #region Private Helpers

        [Conditional("DEBUG")]
        private void checkPropertyName(string propertyName)
        {
            PropertyDescriptor propertyDescriptor = TypeDescriptor.GetProperties(this)[propertyName];
            if (propertyDescriptor == null)
            {
                string message = string.Format(null, "The property with the propertyName '{0}' doesn't exist.", propertyName);
                Debug.Fail(message);

                //throw new InvalidOperationException( string.Format( null, "The property with the propertyName '{0}' doesn't exist.", propertyName ) );
            }
        }

        #endregion // Private Helpers

#if false
        /// <summary>
        /// "Raise" the PropertyChanged-Event (string based)
        /// => It is recommended to use the lambda version or the parameterless version
        /// </summary>
        /// <param name="propertyName"></param>
        protected void RaisePropertyChangedEvent_Deprecated (string propertyName)
        {
            // This is the old "string based" version to raise the PropertyChanged-Event
            // There is no evaluation whether the property name is valid or not !!!
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
#endif
    }
}
