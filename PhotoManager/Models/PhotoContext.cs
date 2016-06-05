using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.EntityClient;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.ComponentModel;
using PhotoManager;
using PhotoManager.Contexts;

namespace System.Data.Objects.DataClasses {
    public class OverrideEntityObject : EntityObject, IDisposable {
        public OverrideEntityObject() {
            PropertyChanged += OrerrideEntityObjectPropertyChanged;
        }

        void OrerrideEntityObjectPropertyChanged(object sender, PropertyChangedEventArgs e) {
            //может тут не надо вызывать событие при детачнутой сущности
            if (PhotoEntitiesContext.Instance.ServerAction != true)
                PhotoEntitiesContext.Instance.OnStateChanged(this, EntityState.Modified);
        }

        public void Dispose() {
            PropertyChanged -= OrerrideEntityObjectPropertyChanged;
        }
    }
}

namespace PhotoManager {
    public class EntityChangedArgs : EventArgs {
        public EntityObject Entity;
        public EntityState State;
    }

    public class PhotoEntitiesContext {
        #region Singleton

        protected PhotoEntitiesContext() {

        }

        public static OverridePhotoEntities Instance {
            get {
                if (_instance == null)
                    _instance = new OverridePhotoEntities();
                return _instance;
            }
        }
        private static OverridePhotoEntities _instance;

        #endregion Singleton

        public static event EventHandler Reloaded;

        /// <summary>
        /// принудительно пересоздать контекст
        /// </summary>
        public static void Reload() {
            if (_instance != null)
                _instance.Dispose();
            _instance = new OverridePhotoEntities();

            if (Reloaded != null)
                Reloaded.Invoke(_instance, new EventArgs());
        }
    }

    /// <summary>
    /// перегруженный класс контекста данных
    /// </summary>
    public class OverridePhotoEntities : PhotoEntities {
        public List<Type> RefreshList = new List<Type>();

        public event EventHandler<EntityChangedArgs> ObjectStateChanged;

        public OverridePhotoEntities() {
            ObjectStateManager.ObjectStateManagerChanged += ObjectStateManagerObjectStateManagerChanged;
        }

        private void ObjectStateManagerObjectStateManagerChanged(object sender, CollectionChangeEventArgs e) {
            if (ObjectStateChanged != null)
                ObjectStateChanged.Invoke(this, new EntityChangedArgs {
                    Entity = (EntityObject)e.Element,
                    State = ((EntityObject)e.Element).EntityState
                });
        }

        public void OnStateChanged(EntityObject entity, EntityState state) {
            if (ObjectStateChanged != null)
                ObjectStateChanged.Invoke(this, new EntityChangedArgs { Entity = entity, State = state });
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
        }
    }

    public delegate void ExecuteMethod();

    //расширенный контекст
    public partial class PhotoEntities {
        public bool ShowError = true;
        public Exception LastError = null;

        public event EventHandler SavedChanges;

        public bool ServerAction;

        /// <summary>
        /// изменён ли контекст
        /// </summary>
        public bool HasChanged() {
            return ObjectStateManager.
                GetObjectStateEntries(EntityState.Added | EntityState.Deleted | EntityState.Modified).Any();
        }

        /// <summary>
        /// возвращает есть ли изменения этого типа
        /// </summary>
        public bool ContainChanging<T>(EntityState entityState) where T : EntityObject {
            return ObjectStateManager.GetObjectStateEntries(entityState).Any(e => e.Entity is T);
        }

        private ExecuteMethod _executeMethod;
        /// <summary>
        /// по окончанию операции выполняем делегат
        /// </summary>
        public void SaveChangedComplitedExecute(ExecuteMethod executeMethod) {
            _executeMethod = executeMethod;
            SavedChanges += EntitiesSavedChanges;
        }
        /// <summary>
        /// закончили и выполнили
        /// </summary>
        private void EntitiesSavedChanges(object sender, EventArgs e) {
            _executeMethod.Invoke();
            SavedChanges -= EntitiesSavedChanges;
        }

        /// <summary>
        /// универсальный *перегруженный метод
        /// </summary>
        public new bool SaveChanges() {
            try {
                IEnumerable<ObjectStateEntry> addedEntities = ObjectStateManager.GetObjectStateEntries(EntityState.Added);
                IEnumerable<ObjectStateEntry> deletedEntities = ObjectStateManager.GetObjectStateEntries(EntityState.Deleted);

                ServerAction = true;
                ((ObjectContext)this).SaveChanges();
                addedEntities.ToList();
                deletedEntities.ToList();
                addedEntities = null;
                deletedEntities = null;
                ServerAction = false;

                if (SavedChanges != null)
                    SavedChanges(this, null);
                return true;
            } catch (UpdateException e) {
                Exception showException = e.InnerException ?? e;
                LoggerContext.Instance.AddError(showException);

                if (ShowError)
                    MessageBox.Show(showException.Message);
                LastError = showException;

                Rollback();

                return false;
            }
        }

        /// <summary>
        /// откатить одну entity
        /// </summary>
        /// <param name="entity"></param>
        private void RevertEntityScalars(EntityObject entity) {
            ServerAction = true;
            var custEntry = ObjectStateManager.GetObjectStateEntry(entity.EntityKey);
            var dri = custEntry.CurrentValues.DataRecordInfo;
            for (var i = 0; i < custEntry.CurrentValues.FieldCount; i++) {
                var propName = dri.FieldMetadata[i].FieldType.Name;
                if (!(from keys in custEntry.EntityKey.EntityKeyValues where keys.Key == propName select keys).Any())
                    custEntry.CurrentValues.SetValue(i, custEntry.OriginalValues[i]);
            }
            ServerAction = false;
        }

        /// <summary>
        /// откатывам все изменения в контексте, 
        /// </summary>
        public void Rollback() {
            ServerAction = true;
            //detaching all added entities
            var addedEntities = ObjectStateManager.GetObjectStateEntries(EntityState.Added);
            foreach (var objectStateEntry in addedEntities)
                Detach(objectStateEntry.Entity);
            var deletedEntities = ObjectStateManager.GetObjectStateEntries(EntityState.Deleted);

            //apply origin value for all modified entities
            foreach (var objectStateEntry in deletedEntities) {
                if (objectStateEntry.Entity != null)
                    ObjectStateManager.ChangeObjectState(objectStateEntry.Entity, EntityState.Modified);
            }

            //apply origin value for all modified entities
            var modifiedEntities = ObjectStateManager.GetObjectStateEntries(EntityState.Modified);
            foreach (var objectStateEntry in modifiedEntities)
                RevertEntityScalars((EntityObject)objectStateEntry.Entity);

            //set state for every changed entities in unchanged 
            AcceptAllChanges();
            ServerAction = false;
        }

        public void ExecuteSql(string sql) {
            var entityConnection = (EntityConnection)Connection;
            DbConnection conn = entityConnection.StoreConnection;

            ConnectionState initialState = conn.State;
            try {
                if (initialState != ConnectionState.Open)
                    conn.Open();  // open connection if not already open
                using (DbCommand cmd = conn.CreateCommand()) {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
            } finally {
                if (initialState != ConnectionState.Open)
                    conn.Close(); // only close connection if not initially open
            }
        }

        public bool TestConnection() {
            var b = new EntityConnectionStringBuilder();
            ConnectionStringSettings entityConString = ConfigurationManager.ConnectionStrings["PhotoEntities"];
            b.ConnectionString = entityConString.ConnectionString;
            string providerConnectionString = b.ProviderConnectionString;

            var conStringBuilder = new SqlConnectionStringBuilder();
            conStringBuilder.ConnectionString = providerConnectionString;
            conStringBuilder.ConnectTimeout = 10;
            string constr = conStringBuilder.ConnectionString;

            using (var conn = new SqlConnection(constr)) {
                try {
                    conn.Open();
                    var command = conn.CreateCommand();
                    command.CommandText = "SELECT 1";
                    command.ExecuteNonQuery();
                    return true;
                } catch {
                    return false;
                }
            }
        }
    }


}