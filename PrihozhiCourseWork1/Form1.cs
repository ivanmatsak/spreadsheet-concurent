using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace PrihozhiCourseWork1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            saveFileDialog1.Filter = "Text files(*.txt)|*.txt|All files(*.*)|*.*";
        }

        private void buttonRes_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            try
            {

                int[] groups = textBox1.Text.Split(' ')
                          .Select(si => int.Parse(si))
                          .ToArray();

                int[] teachers = textBox2.Text.Split(' ')
                        .Select(si => int.Parse(si))
                        .ToArray();


                var list = new List<Lessоn>();
                for (int i = 0; i < groups.Length; i++)
                {
                    for (int j = 0; j < teachers.Length; j++)
                        list.Add(new Lessоn(groups[i], teachers[j]));
                }
                var solver = new Solver();//создаем решатель

                Plan.DaysPerWeek = int.Parse(numericUpDown1.Text);//устанавливаем только два учебных дна - это нужно лишь для данной тестовой задачи, в реальности - выставьте нужное число учебных дней!
                Plan.HoursPerDay = int.Parse(numericUpDown2.Text);

                solver.FitnessFunctions.Add(FitnessFunctions.Windows);//будем штрафовать за окна
                solver.FitnessFunctions.Add(FitnessFunctions.LateLesson);//будем штрафовать за поздние пары

                var res = solver.Solve(list);//находим лучший план

                richTextBox1.Text += res.ToString() + " ";


            }
            catch(Exception ) { MessageBox.Show("Введите правильные данные !\n\n"
            ); }
            
        }

        private async void buttonFile_Click(object sender, EventArgs e)
        {
            /*StreamWriter file = new StreamWriter("output.txt");
            file.Write(Environment.NewLine + "Расписание" + Environment.NewLine);
            file.Write(richTextBox1.Text);
            file.Close();*/
            if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            // получаем выбранный файл
            string filename = saveFileDialog1.FileName;
            // сохраняем текст в файл
            System.IO.File.WriteAllText(filename, "Расписание\n");
            System.IO.File.WriteAllText(filename, richTextBox1.Text);
            MessageBox.Show("Файл сохранен");
        }

        /// <summary>
        /// Фитнесс функции
        /// </summary>
        static class FitnessFunctions
        {
            public static int GroupWindowPenalty = 10;//штраф за окно у группы
            public static int TeacherWindowPenalty = 7;//штраф за окно у преподавателя
            public static int LateLessonPenalty = 1;//штраф за позднюю пару

            public static int LatesetHour = 4;//максимальный час, когда удобно проводить пары

            /// <summary>
            /// Штраф за окна
            /// </summary>
            public static int Windows(Plan plan)
            {
                var res = 0;

                for (byte day = 0; day < Plan.DaysPerWeek; day++)
                {
                    var groupHasLessions = new HashSet<int>();
                    var teacherHasLessions = new HashSet<int>();

                    for (byte hour = 0; hour < Plan.HoursPerDay; hour++)
                    {
                        foreach (var pair in plan.HourPlans[day, hour].GroupToTeacher)
                        {
                            var group = pair.Key;
                            var teacher = pair.Value;
                            if (groupHasLessions.Contains(group) && !plan.HourPlans[day, hour - 1].GroupToTeacher.ContainsKey(group))
                                res += GroupWindowPenalty;
                            if (teacherHasLessions.Contains(teacher) && !plan.HourPlans[day, hour - 1].TeacherToGroup.ContainsKey(teacher))
                                res += TeacherWindowPenalty;

                            groupHasLessions.Add(group);
                            teacherHasLessions.Add(teacher);
                        }
                    }
                }

                return res;
            }

            /// <summary>
            /// Штраф за поздние пары
            /// </summary>
            public static int LateLesson(Plan plan)
            {
                var res = 0;
                foreach (var pair in plan.GetLessons())
                    if (pair.Hour > LatesetHour)
                        res += LateLessonPenalty;

                return res;
            }
        }

        /// <summary>
        /// Решатель (генетический алгоритм)
        /// </summary>
        class Solver
        {
            public int MaxIterations = 1000;
            public int PopulationCount = 100;//должно делиться на 4

            public List<Func<Plan, int>> FitnessFunctions = new List<Func<Plan, int>>();

            public int Fitness(Plan plan)
            {
                var res = 0;

                foreach (var f in FitnessFunctions)
                    res += f(plan);

                return res;
            }

            public Plan Solve(List<Lessоn> pairs)
            {
                //создаем популяцию
                var pop = new Population(pairs, PopulationCount);
                if (pop.Count == 0)
                    MessageBox.Show("Невозможно создать расписание, неверное к-во данных!");
                //
                var count = MaxIterations;
                while (count-- > 0)
                {
                    //считаем фитнесс функцию для всех планов
                    pop.ForEach(p => p.FitnessValue = Fitness(p));
                    //сортруем популяцию по фитнесс функции
                    pop.Sort((p1, p2) => p1.FitnessValue.CompareTo(p2.FitnessValue));
                    //найден идеальный план?
                    if (pop[0].FitnessValue == 0)
                        return pop[0];
                    //отбираем 25% лучших планов
                    pop.RemoveRange(pop.Count / 4, pop.Count - pop.Count / 4);
                    //от каждого создаем трех потомков с мутациями
                    var c = pop.Count;
                    for (int i = 0; i < c; i++)
                    {
                        pop.AddChildOfParent(pop[i]);
                        pop.AddChildOfParent(pop[i]);
                        pop.AddChildOfParent(pop[i]);
                    }
                }

                //считаем фитнесс функцию для всех планов
                pop.ForEach(p => p.FitnessValue = Fitness(p));
                //сортруем популяцию по фитнесс функции
                pop.Sort((p1, p2) => p1.FitnessValue.CompareTo(p2.FitnessValue));

                //возвращаем лучший план
                return pop[0];
            }
        }

        /// <summary>
        /// Популяция планов
        /// </summary>
        class Population : List<Plan>
        {
            public Population(List<Lessоn> pairs, int count)
            {
                var maxIterations = count * 2;

                do
                {
                    var plan = new Plan();
                    if (plan.Init(pairs))
                        Add(plan);
                } while (maxIterations-- > 0 && Count < count);
            }

            public bool AddChildOfParent(Plan parent)
            {
                int maxIterations = 10;

                do
                {
                    var plan = new Plan();
                    if (plan.Init(parent))
                    {
                        Add(plan);
                        return true;
                    }
                } while (maxIterations-- > 0);
                return false;
            }
        }

        /// <summary>
        /// План занятий
        /// </summary>
        class Plan
        {
            public static int DaysPerWeek = 6;//6 учебных дня в неделю
            public static int HoursPerDay = 6;//до 6 пар в день

            static Random rnd = new Random(3);

            /// <summary>
            /// План по дням (первый индекс) и часам (второй индекс)
            /// </summary>
            public HourPlan[,] HourPlans = new HourPlan[DaysPerWeek, HoursPerDay];

            public int FitnessValue { get; internal set; }

            public bool AddLesson(Lessоn les)
            {
                return HourPlans[les.Day, les.Hour].AddLesson(les.Group, les.Teacher);
            }

            public void RemoveLesson(Lessоn les)
            {
                HourPlans[les.Day, les.Hour].RemoveLesson(les.Group, les.Teacher);
            }

            /// <summary>
            /// Добавить группу с преподом на любой день и любой час
            /// </summary>
            public bool AddToAnyDayAndHour(int group, int teacher)
            {
                int maxIterations = 30;
                do
                {
                    var day = (byte)rnd.Next(DaysPerWeek);
                    if (AddToAnyHour(day, group, teacher))
                        return true;
                } while (maxIterations-- > 0);

                return false;//не смогли добавить никуда
            }

            /// <summary>
            /// Добавить группу с преподом на любой час
            /// </summary>
            bool AddToAnyHour(byte day, int group, int teacher)
            {
                for (byte hour = 0; hour < HoursPerDay; hour++)
                {
                    var les = new Lessоn(day, hour, group, teacher);
                    if (AddLesson(les))
                        return true;
                }

                return false;//нет свободных часов в этот день
            }

            /// <summary>
            /// Создание плана по списку пар
            /// </summary>
            public bool Init(List<Lessоn> pairs)
            {
                for (int i = 0; i < HoursPerDay; i++)
                    for (int j = 0; j < DaysPerWeek; j++)
                        HourPlans[j, i] = new HourPlan();

                foreach (var p in pairs)
                    if (!AddToAnyDayAndHour(p.Group, p.Teacher))
                        return false;
                return true;
            }

            /// <summary>
            /// Создание наследника с мутацией
            /// </summary>
            public bool Init(Plan parent)
            {
                //копируем предка
                for (int i = 0; i < HoursPerDay; i++)
                    for (int j = 0; j < DaysPerWeek; j++)
                        HourPlans[j, i] = parent.HourPlans[j, i].Clone();

                //выбираем два случайных дня
                var day1 = (byte)rnd.Next(DaysPerWeek);
                var day2 = (byte)rnd.Next(DaysPerWeek);

                //находим пары в эти дни
                var pairs1 = GetLessonsOfDay(day1).ToList();
                var pairs2 = GetLessonsOfDay(day2).ToList();

                //выбираем случайные пары
                if (pairs1.Count == 0 || pairs2.Count == 0) return false;
                var pair1 = pairs1[rnd.Next(pairs1.Count)];
                var pair2 = pairs2[rnd.Next(pairs2.Count)];

                //создаем мутацию - переставляем случайные пары местами
                RemoveLesson(pair1);//удаляем
                RemoveLesson(pair2);//удаляем
                var res1 = AddToAnyHour(pair2.Day, pair1.Group, pair1.Teacher);//вставляем в случайное место
                var res2 = AddToAnyHour(pair1.Day, pair2.Group, pair2.Teacher);//вставляем в случайное место
                return res1 && res2;
            }

            public IEnumerable<Lessоn> GetLessonsOfDay(byte day)
            {
                for (byte hour = 0; hour < HoursPerDay; hour++)
                    foreach (var p in HourPlans[day, hour].GroupToTeacher)
                        yield return new Lessоn(day, hour, p.Key, p.Value);
            }

            public IEnumerable<Lessоn> GetLessons()
            {
                for (byte day = 0; day < DaysPerWeek; day++)
                    for (byte hour = 0; hour < HoursPerDay; hour++)
                        foreach (var p in HourPlans[day, hour].GroupToTeacher)
                            yield return new Lessоn(day, hour, p.Key, p.Value);
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                for (byte day = 0; day < Plan.DaysPerWeek; day++)
                {
                    sb.AppendFormat("\nКвант времени {0}:\r\n", day +1);
                    for (byte hour = 0; hour < Plan.HoursPerDay; hour++)
                    {
                        sb.AppendFormat("   Тик {0}: ", hour +1);
                        foreach (var p in HourPlans[day, hour].GroupToTeacher)
                            sb.AppendFormat("Задача-Процессор: {0}-{1} ", p.Key, p.Value);
                        sb.AppendLine();
                    }
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// План на час
        /// </summary>
        class HourPlan
        {
            /// <summary>
            /// Хранит пару группа-преподаватель
            /// </summary>
            public Dictionary<int, int> GroupToTeacher = new Dictionary<int, int>();

            /// <summary>
            /// Хранит пару преподаватель-группа
            /// </summary>
            public Dictionary<int, int> TeacherToGroup = new Dictionary<int, int>();

            public bool AddLesson(int group, int teacher)
            {
                if (TeacherToGroup.ContainsKey(teacher) || GroupToTeacher.ContainsKey(group))
                    return false;//в этот час уже есть пара у препода или у группы

                GroupToTeacher[group] = teacher;
                TeacherToGroup[teacher] = group;

                return true;
            }

            public void RemoveLesson(int group, int teacher)
            {
                GroupToTeacher.Remove(group);
                TeacherToGroup.Remove(teacher);
            }

            public HourPlan Clone()
            {
                var res = new HourPlan();
                res.GroupToTeacher = new Dictionary<int, int>(GroupToTeacher);
                res.TeacherToGroup = new Dictionary<int, int>(TeacherToGroup);

                return res;
            }
        }

        /// <summary>
        /// Пара
        /// </summary>
        class Lessоn
        {
            public byte Day = 255;
            public byte Hour = 255;
            public int Group;
            public int Teacher;

            public Lessоn(byte day, byte hour, int group, int teacher)
                : this(group, teacher)
            {
                Day = day;
                Hour = hour;
            }

            public Lessоn(int group, int teacher)
            {
                Group = group;
                Teacher = teacher;
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
