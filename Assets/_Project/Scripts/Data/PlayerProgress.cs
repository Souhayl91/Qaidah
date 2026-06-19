using System.Collections.Generic;

namespace SimpleQaidah.Data
{
    [System.Serializable]
    public class PlayerProgress
    {
        public int totalXP;
        public List<LessonProgress> lessons = new List<LessonProgress>();

        public LessonProgress GetLesson(string lessonId)
        {
            foreach (var lp in lessons)
            {
                if (lp.lessonId == lessonId) return lp;
            }
            return null;
        }

        public LessonProgress GetOrCreateLesson(string lessonId, int groupCount)
        {
            var lp = GetLesson(lessonId);
            if (lp != null) return lp;

            lp = new LessonProgress { lessonId = lessonId };
            for (int i = 0; i < groupCount; i++)
            {
                lp.groups.Add(new GroupProgress
                {
                    groupIndex = i,
                    bestScore = 0,
                    stars = 0,
                    isUnlocked = i == 0,
                    isCompleted = false
                });
            }
            lessons.Add(lp);
            return lp;
        }
    }

    [System.Serializable]
    public class LessonProgress
    {
        public string lessonId;
        public List<GroupProgress> groups = new List<GroupProgress>();
        public bool isCompleted;

        public int TotalStars
        {
            get
            {
                int total = 0;
                foreach (var g in groups) total += g.stars;
                return total;
            }
        }

        public int CompletedGroupCount
        {
            get
            {
                int count = 0;
                foreach (var g in groups)
                {
                    if (g.isCompleted) count++;
                }
                return count;
            }
        }
    }

    [System.Serializable]
    public class GroupProgress
    {
        public int groupIndex;
        public int bestScore;
        public int stars;
        public bool isUnlocked;
        public bool isCompleted;
    }
}
