//========================================================================
// This file was generated using the MyGeneration tool in combination
// with the Gentle.NET Business Entity template, $Rev: 965 $
//========================================================================
using System;
using System.Collections.Generic;
using Gentle.Framework;
using TvLibrary.Log;

namespace TvDatabase
{
  /// <summary>
  /// Instances of this class represent the properties and methods of a row in the table <b>Program</b>.
  /// </summary>
  [TableName("Program")]
  public class Program : Persistent
  {
    #region Members

    private bool isChanged;
    [TableColumn("idProgram", NotNull = true), PrimaryKey(AutoGenerated = true)] private int idProgram;
    [TableColumn("idChannel", NotNull = true), ForeignKey("Channel", "idChannel")] private int idChannel;
    [TableColumn("startTime", NotNull = true)] private DateTime startTime;
    [TableColumn("endTime", NotNull = true)] private DateTime endTime;
    [TableColumn("title", NotNull = true)] private string title;
    [TableColumn("description", NotNull = true)] private string description;
    [TableColumn("genre", NotNull = true)] private string genre;
    [TableColumn("notify", NotNull = true)] private bool notify;
    [TableColumn("originalAirDate", NotNull = true)] private DateTime originalAirDate;
    [TableColumn("seriesNum", NotNull = true)] private string seriesNum;
    [TableColumn("episodeNum", NotNull = true)] private string episodeNum;
    [TableColumn("episodePart", NotNull = true)] private string episodePart;
    [TableColumn("episodeName", NotNull = true)] private string episodeName;
    [TableColumn("starRating", NotNull = true)] private int starRating;
    [TableColumn("classification", NotNull = true)] private string classification;
    [TableColumn("parentalRating", NotNull = true)] private int parentalRating;

    #endregion

    #region Constructors

    /// <summary> 
    /// Create a new object by specifying all fields (except the auto-generated primary key field). 
    /// </summary> 
    public Program(int idChannel, DateTime startTime, DateTime endTime, string title, string description, string genre,
                   bool notify, DateTime originalAirDate, string seriesNum, string episodeNum, string episodeName, string episodePart, int starRating,
                   string classification, int parentalRating)
    {
      isChanged = true;
      this.idChannel = idChannel;
      this.startTime = startTime;
      this.endTime = endTime;
      this.title = title;
      this.description = description;
      this.genre = genre;
      this.notify = notify;
      this.originalAirDate = originalAirDate;
      this.seriesNum = seriesNum;
      this.episodeNum = episodeNum;
      this.episodeName = episodeName;
      this.episodePart = episodePart;
      this.starRating = starRating;
      this.classification = classification;
      this.parentalRating = parentalRating;
    }

    /// <summary> 
    /// Create an object from an existing row of data. This will be used by Gentle to 
    /// construct objects from retrieved rows. 
    /// </summary> 
    public Program(int idProgram, int idChannel, DateTime startTime, DateTime endTime, string title, string description,
                   string genre, bool notify, DateTime originalAirDate, string seriesNum, string episodeNum, string episodeName, string episodePart,
                   int starRating, string classification, int parentalRating)
    {
      this.idProgram = idProgram;
      this.idChannel = idChannel;
      this.startTime = startTime;
      this.endTime = endTime;
      this.title = title;
      this.description = description;
      this.genre = genre;
      this.notify = notify;
      this.originalAirDate = originalAirDate;
      this.seriesNum = seriesNum;
      this.episodeNum = episodeNum;
      this.episodeName = episodeName;
      this.episodePart = episodePart;
      this.starRating = starRating;
      this.classification = classification;
      this.parentalRating = parentalRating;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Indicates whether the entity is changed and requires saving or not.
    /// </summary>
    public bool IsChanged
    {
      get { return isChanged; }
    }

    /// <summary>
    /// Property relating to database column idProgram
    /// </summary>
    public int IdProgram
    {
      get { return idProgram; }
    }

    /// <summary>
    /// Property relating to database column idChannel
    /// </summary>
    public int IdChannel
    {
      get { return idChannel; }
      set
      {
        isChanged |= idChannel != value;
        idChannel = value;
      }
    }

    /// <summary>
    /// Property relating to database column startTime
    /// </summary>
    public DateTime StartTime
    {
      get { return startTime; }
      set
      {
        isChanged |= startTime != value;
        startTime = value;
      }
    }

    /// <summary>
    /// Property relating to database column endTime
    /// </summary>
    public DateTime EndTime
    {
      get { return endTime; }
      set
      {
        isChanged |= endTime != value;
        endTime = value;
      }
    }

    /// <summary>
    /// Property relating to database column title
    /// </summary>
    public string Title
    {
      get { return title; }
      set
      {
        isChanged |= title != value;
        title = value;
      }
    }

    /// <summary>
    /// Property relating to database column title and episode info (readonly)
    /// </summary>
    public string TitleDisplay
    {
      get
      {
        return TVDatabase.Utils.TitleDisplay(title,episodeName,seriesNum,episodeNum,episodePart);
      }
    }

    /// <summary>
    /// Property relating to database column description
    /// </summary>
    public string Description
    {
      get { return description; }
      set
      {
        isChanged |= description != value;
        description = value;
      }
    }

    /// <summary>
    /// Property relating to database column genre
    /// </summary>
    public string Genre
    {
      get { return genre; }
      set
      {
        isChanged |= genre != value;
        genre = value;
      }
    }

    /// <summary>
    /// Property relating to database column notify
    /// </summary>
    public bool Notify
    {
      get { return notify; }
      set
      {
        isChanged |= notify != value;
        notify = value;
      }
    }

    /// <summary>
    /// The original air date of the program
    /// </summary>
    public DateTime OriginalAirDate
    {
      get { return originalAirDate; }
      set
      {
        isChanged |= originalAirDate != value;
        originalAirDate = value;
      }
    }

    /// <summary>
    /// The programs series id
    /// </summary>
    public string SeriesNum
    {
      get { return seriesNum; }
      set
      {
        isChanged |= seriesNum != value;
        seriesNum = value;
      }
    }

    /// <summary>
    /// The programs episode number from the distributor
    /// </summary>
    public string EpisodeNum
    {
      get { return episodeNum; }
      set
      {
        isChanged |= episodeNum != value;
        episodeNum = value;
      }
    }

    /// <summary>
    /// The programs episode title from the distributor
    /// </summary>
    public string EpisodeName
    {
      get { return episodeName; }
      set
      {
        isChanged |= episodeName != value;
        episodeName = value;
      }
    }

    /// <summary>
    /// The programs episode part from the distributor
    /// </summary>
    public string EpisodePart
    {
      get { return episodePart; }
      set
      {
        isChanged |= episodePart != value;
        episodePart = value;
      }
    }
    /// <summary>
    /// Property relating to database column seriesNum, episodeNum and episodePart (readonly)
    /// </summary>
    public string EpisodeNumber
    {
      get
      {
        return TVDatabase.Utils.GetEpisodeNumber(seriesNum, episodeNum, episodePart);
      }
    }

    /// <summary>
    /// A rating for a movie (EPG standard is 0/5 stars = 10)
    /// </summary>
    public int StarRating
    {
      get { return starRating; }
      set
      {
        isChanged |= starRating != value;
        starRating = value;
      }
    }

    /// <summary>
    /// A rating for a program (e.g. USK, FSK, etc)
    /// </summary>
    public string Classification
    {
      get { return classification; }
      set
      {
        isChanged |= classification != value;
        classification = value;
      }
    }

    /// <summary>
    /// A parental guidance rating for a program (the nr gives the min. age suggested to watch this program)
    /// </summary>
    public int ParentalRating
    {
      get { return parentalRating; }
      set
      {
        isChanged |= parentalRating != value;
        parentalRating = value;
      }
    }

    #endregion

    #region Storage and Retrieval

    /// <summary>
    /// Static method to retrieve all instances that are stored in the database in one call
    /// </summary>
    public static IList<Program> ListAll()
    {
      return Broker.RetrieveList<Program>();
    }

    /// <summary>
    /// Retrieves an entity given it's id.
    /// </summary>
    public static Program Retrieve(int id)
    {
      // Return null if id is smaller than seed and/or increment for autokey
      if (id < 1)
      {
        return null;
      }
      Key key = new Key(typeof (Program), true, "idProgram", id);
      return Broker.RetrieveInstance<Program>(key);
    }

    /// <summary>
    /// Retreives the first found instance of a Program given its Title,Start and End Times 
    /// </summary>
    /// <param name="title">Title we wanna look for</param>
    /// <param name="startTime">StartTime</param>
    /// <param name="endTime">EndTime</param>
    /// <returns></returns>
    public static Program RetrieveByTitleAndTimes(string title, DateTime startTime, DateTime endTime)
    {
      //select * from 'foreigntable'
      SqlBuilder sb = new SqlBuilder(StatementType.Select, typeof (Program));

      // where foreigntable.foreignkey = ourprimarykey
      sb.AddConstraint(Operator.Equals, "Title", title);
      sb.AddConstraint(Operator.Equals, "startTime", startTime);
      sb.AddConstraint(Operator.Equals, "endTime", endTime);
      // passing true indicates that we'd like a list of elements, i.e. that no primary key
      // constraints from the type being retrieved should be added to the statement
      SqlStatement stmt = sb.GetStatement(true);

      // execute the statement/query and create a collection of User instances from the result set
      IList<Program> result = ObjectFactory.GetCollection<Program>(stmt.Execute());
      if (result == null)
      {
        return null;
      }
      if (result.Count == 0)
      {
        return null;
      }
      return result[0];

      // TODO In the end, a GentleList should be returned instead of an arraylist
      //return new GentleList( typeof(ChannelMap), this );
    }

    /// <summary>
    /// Retreives the first found instance of a Program given its Title,Start and End Times and referenced channel
    /// </summary>
    /// <param name="title">Title we wanna look for</param>
    /// <param name="startTime">StartTime</param>
    /// <param name="endTime">EndTime</param>
    /// <param name="channelId">Referenced Channel id</param>
    /// <returns></returns>
    public static Program RetrieveByTitleTimesAndChannel(string title, DateTime startTime, DateTime endTime, int channelId)
    {
      //select * from 'foreigntable'
      SqlBuilder sb = new SqlBuilder(StatementType.Select, typeof(Program));

      // where foreigntable.foreignkey = ourprimarykey
      sb.AddConstraint(Operator.Equals, "Title", title);
      sb.AddConstraint(Operator.Equals, "startTime", startTime);
      sb.AddConstraint(Operator.Equals, "endTime", endTime);
      sb.AddConstraint(Operator.Equals, "idChannel", channelId);
      // passing true indicates that we'd like a list of elements, i.e. that no primary key
      // constraints from the type being retrieved should be added to the statement
      SqlStatement stmt = sb.GetStatement(true);

      // execute the statement/query and create a collection of User instances from the result set
      IList<Program> result = ObjectFactory.GetCollection<Program>(stmt.Execute());
      if (result == null)
      {
        return null;
      }
      if (result.Count == 0)
      {
        return null;
      }
      return result[0];

      // TODO In the end, a GentleList should be returned instead of an arraylist
      //return new GentleList( typeof(ChannelMap), this );
    }

    /// <summary>
    /// Retreives the first found instance of a Program given its Title
    /// </summary>
    /// <param name="title">Title we wanna look for</param>
    /// <returns></returns>
    public static IList<Program> RetrieveByTitle(string title)
    {
      //select * from 'foreigntable'
      SqlBuilder sb = new SqlBuilder(StatementType.Select, typeof (Program));

      // where foreigntable.foreignkey = ourprimarykey
      sb.AddConstraint(Operator.Equals, "Title", title);
      // passing true indicates that we'd like a list of elements, i.e. that no primary key
      // constraints from the type being retrieved should be added to the statement
      SqlStatement stmt = sb.GetStatement(true);

      // execute the statement/query and create a collection of User instances from the result set
      //return (Program)ObjectFactory.GetCollection(typeof(Program), stmt.Execute())[0];
      return ObjectFactory.GetCollection<Program>(stmt.Execute());

      // TODO In the end, a GentleList should be returned instead of an arraylist
      //return new GentleList( typeof(ChannelMap), this );
    }

    /// <summary>
    /// Retreives any current running Program given its Title , using pre and post recording times 
    /// </summary>
    /// <param name="title">Title we wanna look for</param>
    /// <param name="preRec">Pre recording value</param>
    /// <param name="postRec">Post recording value</param>
    /// <returns></returns>
    public static IList<Program> RetrieveCurrentRunningByTitle(string title, int preRec, int postRec)
    {
      //select * from 'foreigntable'
      SqlBuilder sb = new SqlBuilder(StatementType.Select, typeof (Program));
      sb.AddConstraint(Operator.Equals, "Title", title);
      sb.AddConstraint(Operator.LessThanOrEquals, "startTime", DateTime.Now.AddMinutes(preRec));
      sb.AddConstraint(Operator.GreaterThan, "endTime", DateTime.Now);
      // passing true indicates that we'd like a list of elements, i.e. that no primary key
      // constraints from the type being retrieved should be added to the statement
      SqlStatement stmt = sb.GetStatement(true);

      // execute the statement/query and create a collection of User instances from the result set
      return ObjectFactory.GetCollection<Program>(stmt.Execute());

      // TODO In the end, a GentleList should be returned instead of an arraylist
      //return new GentleList( typeof(ChannelMap), this );
    }

    /// <summary>
    /// Retreives the programs with a given title and starting between given Start and End Times 
    /// </summary>
    /// <param name="title">Title we wanna look for</param>
    /// <param name="startTime">StartTime</param>
    /// <param name="endTime">EndTime</param>
    /// <returns></returns>
    public static IList<Program> RetrieveByTitleAndTimesInterval(string title, DateTime startTime, DateTime endTime)
    {
      //select * from 'foreigntable'
      SqlBuilder sb = new SqlBuilder(StatementType.Select, typeof (Program));

      // where foreigntable.foreignkey = ourprimarykey
      sb.AddConstraint(Operator.Equals, "Title", title);
      sb.AddConstraint(Operator.GreaterThanOrEquals, "startTime", startTime);
      sb.AddConstraint(Operator.LessThanOrEquals, "startTime", endTime);
      // passing true indicates that we'd like a list of elements, i.e. that no primary key
      // constraints from the type being retrieved should be added to the statement
      SqlStatement stmt = sb.GetStatement(true);

      // execute the statement/query and create a collection of User instances from the result set
      return ObjectFactory.GetCollection<Program>(stmt.Execute());

      // TODO In the end, a GentleList should be returned instead of an arraylist
      //return new GentleList( typeof(ChannelMap), this );
    }

    /// <summary>
    /// Retrieves an entity given it's id, using Gentle.Framework.Key class.
    /// This allows retrieval based on multi-column keys.
    /// </summary>
    public static Program Retrieve(Key key)
    {
      return Broker.RetrieveInstance<Program>(key);
    }

    /// <summary>
    /// Persists the entity if it was never persisted or was changed.
    /// </summary>
    public override void Persist()
    {
      if (IsChanged || !IsPersisted)
      {
        try
        {
          base.Persist();
        }
        catch (Exception ex)
        {
          Log.Error("Exception in Program.Persist() with Message {0}", ex.Message);
          return;
        }
        isChanged = false;
      }
    }

    #endregion

    #region Relations

    /// <summary>
    /// Get a list of Favorite referring to the current entity.
    /// </summary>
    public IList<Favorite> ReferringFavorite()
    {
      //select * from 'foreigntable'
      SqlBuilder sb = new SqlBuilder(StatementType.Select, typeof (Favorite));

      // where foreigntable.foreignkey = ourprimarykey
      sb.AddConstraint(Operator.Equals, "idProgram", idProgram);

      // passing true indicates that we'd like a list of elements, i.e. that no primary key
      // constraints from the type being retrieved should be added to the statement
      SqlStatement stmt = sb.GetStatement(true);

      // execute the statement/query and create a collection of User instances from the result set
      return ObjectFactory.GetCollection<Favorite>(stmt.Execute());

      // TODO In the end, a GentleList should be returned instead of an arraylist
      //return new GentleList( typeof(Favorite), this );
    }

    /// <summary>
    ///
    /// </summary>
    public Channel ReferencedChannel()
    {
      return Channel.Retrieve(IdChannel);
    }

    #endregion

    public void Delete()
    {
      IList<Favorite> list = ReferringFavorite();
      foreach (Favorite favorite in list)
      {
        favorite.Remove();
      }
      Remove();
    }

    /// <summary>
    /// Checks if the program is running between the specified start and end time/dates, i.e. whether the intervals overlap
    /// </summary>
    /// <param name="tStartTime">Start date and time</param>
    /// <param name="tEndTime">End date and time</param>
    /// <returns>true if program is running between tStartTime-tEndTime</returns>
    public bool RunningAt(DateTime tStartTime, DateTime tEndTime)
    {
      // do NOT use <= >= since end-times are non-including
      return tStartTime < EndTime && tEndTime > StartTime;
    }

    /// <summary>
    /// Checks if the program is running at the specified date/time
    /// </summary>
    /// <param name="tCurTime">date and time</param>
    /// <returns>true if program is running at tCurTime</returns>
    public bool IsRunningAt(DateTime tCurTime)
    {
      bool bRunningAt = false;
      if (tCurTime >= StartTime && tCurTime <= EndTime)
      {
        bRunningAt = true;
      }
      return bRunningAt;
    }

    public Program Clone()
    {
      Program p = new Program(idChannel, StartTime, EndTime, Title, Description, Genre, Notify, OriginalAirDate,
                              SeriesNum, EpisodeNum, EpisodeName, EpisodePart, StarRating, Classification, parentalRating);
      return p;
    }

    public override string ToString()
    {
      return String.Format("{0}(ID:{1}) on {2} {3} - {4}", Title, idProgram, IdChannel, StartTime, EndTime);
    }
  }
}