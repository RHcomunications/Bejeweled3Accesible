using System;
using System.Collections.Generic;

namespace Bejeweled3Accessible.Engine
{
    public enum AffirmationTheme
    {
        General,
        PositiveThinking,
        Prosperity,
        QuitBadHabits,
        SelfConfidence,
        WeightLoss
    }

    // Complete affirmation bank from the original Bejeweled 3 Zen mode
    // (affirmations folder). Each entry holds the Spanish translation first
    // and the original English phrase second, so both languages are available.
    public static class Affirmations
    {
        private static readonly Tuple<string, string>[] General =
        {
            Tuple.Create("Dejo pasar el miedo.", "I let fear pass me by."),
            Tuple.Create("Tengo un destino.", "I have a destiny."),
            Tuple.Create("Acepto que las cosas cambian y terminan.", "I accept that things change and end."),
            Tuple.Create("Amo con valentía.", "I love courageously."),
            Tuple.Create("Estoy a la altura de cualquier situación.", "I am up to any situation."),
            Tuple.Create("Acepto la vida tal como es.", "I accept life as it is."),
            Tuple.Create("Valoro el amor que tengo.", "I appreciate the love I have."),
            Tuple.Create("Me siento profundamente amado por muchas personas.", "I feel deeply loved by many people."),
            Tuple.Create("Cada día soy más valiente.", "I am more courageous every day."),
            Tuple.Create("Confío en que siempre encontraré una alternativa.", "I trust that I will always find an alternative."),
            Tuple.Create("Tengo valor e ingenio.", "I have courage and wit."),
            Tuple.Create("Estoy en paz.", "I am peaceful."),
            Tuple.Create("Muestro serenidad bajo presión.", "I show grace under pressure."),
            Tuple.Create("Soy decidido y seguro de mí mismo.", "I am decisive and self-confident."),
            Tuple.Create("Me atrevo a ser yo mismo.", "I dare to be myself."),
            Tuple.Create("El valor surge en mí automáticamente.", "Courage automatically rises in me."),
            Tuple.Create("Vivo la vida que refleja mis deseos más profundos.", "I live the life that reflects my deepest wishes."),
            Tuple.Create("Suelto mis miedos.", "I let go of my fears."),
            Tuple.Create("Tengo fuerza e ingenio para resolver.", "I have strength and resourcefulness."),
            Tuple.Create("Asumo riesgos.", "I take risks."),
            Tuple.Create("Soy digno de cariño.", "I am lovable."),
            Tuple.Create("Merezco ser amado.", "I am worth loving."),
            Tuple.Create("Estoy libre de pensamientos y sentimientos negativos.", "I am free of negative thoughts and feelings."),
            Tuple.Create("El amor disuelve el miedo.", "Love dissolves fear."),
            Tuple.Create("Confío en el proceso de la vida.", "I trust the process of life."),
            Tuple.Create("Todas las respuestas están dentro de mí.", "All answers are within me."),
            Tuple.Create("Sigo mi sabiduría interior.", "I follow my inner wisdom."),
            Tuple.Create("Estoy a salvo.", "I am safe."),
            Tuple.Create("El Universo me apoya.", "The Universe supports me."),
            Tuple.Create("El amor es un estado del ser.", "Love is a state of being."),
            Tuple.Create("Mi amor se desborda hacia los demás.", "My love overflows to others."),
            Tuple.Create("La vida es el AHORA.", "Life is about NOW."),
            Tuple.Create("Estoy en paz con mi pasado.", "I am at peace with my past."),
            Tuple.Create("Libero toda la ira.", "I release all anger."),
            Tuple.Create("Merezco lo mejor.", "I deserve the best."),
            Tuple.Create("Me relajo en mi bienestar natural.", "I relax into my natural well-being."),
            Tuple.Create("Comprendo a los demás, no los juzgo.", "I understand others, not judge others."),
            Tuple.Create("Libero la energía tóxica del miedo.", "I release the toxic energy of fear."),
            Tuple.Create("Libero todas las críticas.", "I release all criticisms."),
            Tuple.Create("El amor fluye a cada célula de mi cuerpo.", "Love flows to every cell in my body."),
            Tuple.Create("El amor sostiene mi bienestar.", "Love supports my well being."),
            Tuple.Create("Estoy dispuesto a soltar.", "I am willing to let go."),
            Tuple.Create("Todo está bien en mi mundo.", "All is well in my world."),
            Tuple.Create("Soy flexible.", "I am flexible."),
            Tuple.Create("El bienestar es mi estado natural.", "Wellness is my natural state of being."),
            Tuple.Create("Mis creencias crean mi realidad.", "My beliefs create my reality."),
            Tuple.Create("Mis pensamientos son más poderosos que mis creencias.", "My thoughts are more powerful than my beliefs."),
            Tuple.Create("Pienso pensamientos positivos y edificantes.", "I think positive uplifting thoughts."),
            Tuple.Create("Mis acciones crean cambios positivos.", "My actions create positive changes.")
        };

        private static readonly Tuple<string, string>[] PositiveThinking =
        {
            Tuple.Create("Me resulta fácil desechar los pensamientos negativos sobre mí mismo.", "I find it easy to discard negative thoughts about myself."),
            Tuple.Create("Siempre pienso en mí de forma totalmente positiva.", "I always think of myself in a totally positive way."),
            Tuple.Create("Espero triunfar porque soy una persona exitosa.", "I expect to succeed because I am a successful person."),
            Tuple.Create("Triunfaré porque merezco triunfar.", "I will succeed because I deserve to succeed."),
            Tuple.Create("Me siento atraído hacia el éxito.", "I am drawn towards success."),
            Tuple.Create("Persevero con todos mis esfuerzos hacia el éxito.", "I persevere with all my efforts towards success."),
            Tuple.Create("Soy rápido para ver y aprovechar todas las oportunidades de éxito.", "I am quick to see and use all opportunities for success."),
            Tuple.Create("Planifico el éxito y, por tanto, lo encuentro con facilidad.", "I plan for success and, therefore, easily find success."),
            Tuple.Create("Mi personalidad naturalmente exitosa garantiza mi éxito natural.", "My naturally successful personality ensures my natural success."),
            Tuple.Create("Me resulta fácil lograr mis metas y las fijo en alto.", "I find it easy to achieve my goals and I set my goals high."),
            Tuple.Create("Mis metas se acercan con cada día que pasa.", "My goals become closer with every day that passes."),
            Tuple.Create("Siempre logro lo que me propongo lograr.", "I always achieve what I set out to achieve."),
            Tuple.Create("Cada día que pasa me vuelvo constantemente más seguro.", "I become steadily more confident with each day that passes."),
            Tuple.Create("Soy una persona valiosa y digna de cariño.", "I am a worthwhile and loveable person."),
            Tuple.Create("Soy una persona naturalmente popular y transmito calidez a los demás.", "I am a naturally popular individual and I exude warmth to others."),
            Tuple.Create("Me gusto y estoy contento con todo lo que hago.", "I like myself and I'm pleased with everything I do."),
            Tuple.Create("Me apruebo a mí mismo.", "I approve of myself."),
            Tuple.Create("Soy mi propia persona, igual a todos los demás.", "I am my own person, the equal of all others."),
            Tuple.Create("Soy franco, digno de confianza y honesto en todos mis tratos.", "I am forthright, trustworthy, and honest in all my dealings."),
            Tuple.Create("Enfrento miedos y responsabilidades con facilidad.", "I face fears and responsibilities with ease."),
            Tuple.Create("Soy una persona naturalmente afortunada.", "I am a naturally lucky person."),
            Tuple.Create("Espero que las cosas salgan bien, y salen.", "I expect things to work out well, and they do."),
            Tuple.Create("Naturalmente me muevo hacia las soluciones en lugar de alejarme de los problemas.", "I naturally move towards solutions rather than away from problems."),
            Tuple.Create("Soy naturalmente una persona orientada a las soluciones.", "I am naturally a solution-orientated individual."),
            Tuple.Create("Tomo decisiones con rapidez.", "I make decisions quickly."),
            Tuple.Create("Logro resultados asombrosos con todo lo que hago.", "I achieve amazing results with everything I do."),
            Tuple.Create("Mi mente es un pozo sin fondo de ideas creativas.", "My mind is a bottomless well of creative ideas."),
            Tuple.Create("Corrientes de pensamiento creativo fluyen constantemente en mi mente.", "Streams of creative thought flow constantly in my mind."),
            Tuple.Create("Siempre encuentro soluciones que funcionan.", "I always find solutions that work."),
            Tuple.Create("Siempre estoy relajado y seguro en todo lo que hago.", "I am always relaxed and assured in everything I do."),
            Tuple.Create("Soy eficaz en todos mis empeños.", "I am effective in all my undertakings."),
            Tuple.Create("Otras personas se inspiran con mis esfuerzos.", "Other people are inspired by my efforts."),
            Tuple.Create("Puedo concentrarme plenamente en cualquier cosa que desee con facilidad.", "I can easily bring great concentration to bear on anything I wish."),
            Tuple.Create("Mi poder de concentración se fortalece cada día.", "My powers of concentration become stronger every day."),
            Tuple.Create("Mi memoria mejora conforme mi concentración se vuelve más intensa.", "My memory improves as my concentration becomes more intense."),
            Tuple.Create("Mi memoria mejora cada día; cuanto más la uso, mejor se vuelve.", "My memory grows better every day; the more I use it, the better it gets."),
            Tuple.Create("Poseo gran energía; cuanto más la uso, más tengo.", "I possess great energy; the more I use it, the more I have."),
            Tuple.Create("Soy naturalmente entusiasta y el entusiasmo me da energía.", "I am naturally enthusiastic and enthusiasm gives me energy."),
            Tuple.Create("Soy un optimista natural; siempre puedo convertir un revés en una ventaja.", "I am a natural optimist; I can always turn a setback into an advantage."),
            Tuple.Create("Soy una persona dinámica y persigo mis metas con energía.", "I am a dynamic person and I pursue my goals energetically."),
            Tuple.Create("Pienso en lo que quiero que suceda.", "I think of what I want to happen."),
            Tuple.Create("Pienso en cómo quiero ser.", "I think of how I want to be."),
            Tuple.Create("Espero con interés el cambio beneficioso y lo disfruto.", "I look forward to beneficial change and I enjoy it."),
            Tuple.Create("Hago todo lo necesario para lograr mis metas.", "I do everything that is necessary to achieve my goals."),
            Tuple.Create("Estoy tranquilo y relajado.", "I am calm and relaxed."),
            Tuple.Create("Mis pensamientos son edificantes y nutritivos.", "My thoughts are uplifting and nurturing."),
            Tuple.Create("Disfruto pensando pensamientos positivos.", "I enjoy thinking positive thoughts."),
            Tuple.Create("Me siento bien conmigo mismo y con mi vida.", "I feel good about myself and my life."),
            Tuple.Create("Merezco sentirme bien ahora mismo.", "I deserve to feel good right now."),
            Tuple.Create("Me siento en paz y tranquilo.", "I feel peaceful and calm."),
            Tuple.Create("Mi respiración es lenta y serena.", "My breathing is slow and calm."),
            Tuple.Create("Mis músculos están relajados y cómodos.", "My muscles are relaxed and comfortable."),
            Tuple.Create("Estoy centrado y plenamente presente.", "I am grounded and fully present."),
            Tuple.Create("Manejo eficazmente cualquier situación que se me presente.", "I effectively handle any situation that comes my way."),
            Tuple.Create("Encuentro soluciones a mis asuntos emocionales con facilidad y calma.", "I find solutions to my emotional issues with ease and calmness."),
            Tuple.Create("Agradezco todas las cosas buenas de mi vida.", "I am grateful for all the good things in my life."),
            Tuple.Create("Practico los métodos de relajación que disfruto.", "I practice the relaxation methods that I enjoy.")
        };

        private static readonly Tuple<string, string>[] Prosperity =
        {
            Tuple.Create("Merezco la abundancia.", "I deserve abundance."),
            Tuple.Create("Vivo en un mundo abundante.", "I live in an abundant world."),
            Tuple.Create("Soy próspero.", "I am prosperous."),
            Tuple.Create("Soy generoso conmigo mismo.", "I am generous to myself."),
            Tuple.Create("El Universo es seguro, abundante y amistoso.", "The Universe is safe, abundant and friendly."),
            Tuple.Create("Me permito ser exitoso.", "I allow myself to be successful."),
            Tuple.Create("Elijo creencias que apoyan mis metas.", "I choose beliefs that support my goals."),
            Tuple.Create("Siempre entra más dinero del que sale.", "I always have more money coming in than going out."),
            Tuple.Create("Acepto la abundancia en mi vida.", "I accept abundance in my life."),
            Tuple.Create("Creo un trabajo maravilloso y nuevo.", "I create a wonderful, new job."),
            Tuple.Create("Estoy abierto a nuevas oportunidades.", "I am open to new opportunities."),
            Tuple.Create("Confío en mi sabiduría interior.", "I trust my inner wisdom."),
            Tuple.Create("Pienso y sueño sin límites.", "I think and dream in unlimited ways."),
            Tuple.Create("Permito que el bien llegue a mi vida.", "I allow good to come into my life."),
            Tuple.Create("Triunfaré.", "I will succeed."),
            Tuple.Create("Acepto la opulencia.", "I accept affluence."),
            Tuple.Create("Merezco la prosperidad.", "I deserve prosperity."),
            Tuple.Create("Atraigo oportunidades.", "I attract opportunity."),
            Tuple.Create("Soy rico.", "I am wealthy."),
            Tuple.Create("La riqueza fluye hacia mi vida.", "Riches flow into my life."),
            Tuple.Create("Tengo facilidad para crear valor y riqueza.", "I have a knack for creating value and wealth."),
            Tuple.Create("Mi vida está llena de alegría.", "My life is full of joy."),
            Tuple.Create("Merezco la buena vida.", "I deserve the good life."),
            Tuple.Create("Espero el éxito.", "I expect success."),
            Tuple.Create("Aprendo de los demás.", "I learn from others."),
            Tuple.Create("Confío en mis capacidades y habilidades.", "I am confident in my abilities and skills."),
            Tuple.Create("Los sueños pueden hacerse realidad.", "Dreams can come true."),
            Tuple.Create("Convierto las ideas en acción.", "I turn ideas into action."),
            Tuple.Create("Creo riqueza con facilidad y sin esfuerzo.", "I create wealth easily and effortlessly."),
            Tuple.Create("Hago que sucedan grandes cosas.", "I make great things happen."),
            Tuple.Create("Me encanta ayudar a las personas.", "I love to help people."),
            Tuple.Create("Doy generosamente y recibo con gratitud.", "I give generously and receive graciously."),
            Tuple.Create("Se me ocurren nuevas ideas para ganar dinero.", "I come up with new ideas for making money."),
            Tuple.Create("A las personas les encanta ayudarme.", "People love to help me."),
            Tuple.Create("Reconozco las oportunidades a mi alrededor.", "I recognize opportunities all around me."),
            Tuple.Create("Merezco ser libre.", "I deserve to be free."),
            Tuple.Create("Crear es divertido.", "It is fun to create."),
            Tuple.Create("Comparto mis ideas con personas que pueden ayudarme.", "I share my ideas with people who can help me."),
            Tuple.Create("Soy creativo.", "I am creative."),
            Tuple.Create("Merezco el éxito.", "I deserve success."),
            Tuple.Create("Soy valioso.", "I am valuable."),
            Tuple.Create("Soy importante.", "I am important."),
            Tuple.Create("Me preocupo por los demás.", "I care for others."),
            Tuple.Create("Hago lo correcto.", "I do the right thing."),
            Tuple.Create("Soy recompensado por mis esfuerzos.", "I am rewarded for my efforts."),
            Tuple.Create("Tengo el valor necesario para triunfar.", "I have the courage it takes to succeed."),
            Tuple.Create("Vivo una vida de abundancia.", "I live a life of abundance.")
        };

        private static readonly Tuple<string, string>[] QuitBadHabits =
        {
            Tuple.Create("Estoy recuperando el control de mi vida y mi salud.", "I am taking back control of my life and my health."),
            Tuple.Create("Este sentimiento pasará.", "This feeling will pass."),
            Tuple.Create("Estoy invirtiendo en mí mismo y en el futuro.", "I am making an investment in myself and the future."),
            Tuple.Create("Este momento de incomodidad vale la pena por todas las recompensas.", "This moment of discomfort is worth it for all the rewards."),
            Tuple.Create("Tendré éxito en mi esfuerzo por dejarlo.", "I will succeed in my effort to quit."),
            Tuple.Create("Estoy tranquilo y en control.", "I am calm and controlled."),
            Tuple.Create("Tengo el control de mi propia vida.", "I am in control of my own life."),
            Tuple.Create("Estoy sereno y sin perturbaciones.", "I am serene and undisturbed."),
            Tuple.Create("Quiero sentirme sano.", "I want to feel healthy."),
            Tuple.Create("Mi salud es lo más importante.", "My health is the most important thing."),
            Tuple.Create("Cada vez que me abstengo, me vuelvo más fuerte.", "Every time I refrain I grow stronger."),
            Tuple.Create("No necesito mis malos hábitos.", "I don't need my bad habits."),
            Tuple.Create("Seré más feliz cuando esté libre de mis malos hábitos.", "I'll be happier when I'm free of my bad habits."),
            Tuple.Create("Soy más fuerte que mi debilidad.", "I am stronger than my weakness."),
            Tuple.Create("Se siente bien ejercitar mi fuerza de voluntad.", "It feels good to exercise my will power."),
            Tuple.Create("Puedo aguantar un poco más.", "I can hold off a while longer."),
            Tuple.Create("Un día a la vez.", "One day at a time."),
            Tuple.Create("Si me equivoco, no es el fin del mundo.", "If I slip it's not the end of the world."),
            Tuple.Create("Mis amigos quieren que triunfe.", "My friends want me to succeed."),
            Tuple.Create("Mi familia me apoya.", "My family is rooting for me."),
            Tuple.Create("Invertiré tiempo y energía en lo que importa.", "I will spend time and energy on what matters."),
            Tuple.Create("Venceré mis defectos.", "I will defeat my flaws."),
            Tuple.Create("Espero mi vida con ilusión.", "I look forward to my life."),
            Tuple.Create("Es trabajo duro, pero vale la pena.", "It's hard work but it's worth it."),
            Tuple.Create("Soy un ganador.", "I am a winner."),
            Tuple.Create("Respeto mi cuerpo y a mí mismo.", "I respect my body and my self."),
            Tuple.Create("Puedo hacerlo.", "I can do this."),
            Tuple.Create("Si fallo, me levantaré de nuevo.", "If I fail I will pick myself up again."),
            Tuple.Create("Mi vida es valiosa.", "My life is valuable."),
            Tuple.Create("Los malos hábitos son solo distracciones.", "Bad habits are only distractions."),
            Tuple.Create("Es difícil, pero puedo lograrlo.", "It's tough but I can do it."),
            Tuple.Create("Dejarlo es su propia recompensa.", "Quitting is its own reward."),
            Tuple.Create("Me gusta demostrar que puedo triunfar.", "I like proving I can succeed."),
            Tuple.Create("Estoy orgulloso de mí mismo.", "I am proud of myself."),
            Tuple.Create("Las personas respetan mi compromiso.", "People respect my commitment."),
            Tuple.Create("Mantener la disciplina es una sensación satisfactoria.", "Holding to discipline is a satisfying feeling.")
        };

        private static readonly Tuple<string, string>[] SelfConfidence =
        {
            Tuple.Create("Soy fuerte y estoy seguro.", "I am strong and secure."),
            Tuple.Create("Tengo recursos interiores.", "I have inner resources."),
            Tuple.Create("Tengo el poder de realizar mis metas.", "I have the power to realize my goals."),
            Tuple.Create("Me hago cargo de mi vida.", "I take charge of my life."),
            Tuple.Create("Soy valioso.", "I am valuable."),
            Tuple.Create("Me sostengo firmemente en la creencia en mí mismo.", "I stand firmly in my belief in myself."),
            Tuple.Create("Las personas me respetan.", "People respect me."),
            Tuple.Create("Les caigo bien a las personas.", "People like me."),
            Tuple.Create("Soy un verdadero amigo.", "I am a true friend."),
            Tuple.Create("Me preocupo por las personas.", "I care for people."),
            Tuple.Create("Confío en mis capacidades, experiencia y conocimientos.", "I am confident of my capabilities, expertise, and know-how."),
            Tuple.Create("Me interesan los demás.", "I am interested in others."),
            Tuple.Create("Tengo confianza.", "I am confident."),
            Tuple.Create("Soy una persona fuerte.", "I am a strong person."),
            Tuple.Create("Disuelvo todos los obstáculos para tener una confianza total en mí mismo.", "I dissolve all obstacles to having complete self-confidence."),
            Tuple.Create("Soy un éxito.", "I am a success."),
            Tuple.Create("Elijo ser feliz.", "I choose to be happy."),
            Tuple.Create("Soy valiente.", "I am courageous."),
            Tuple.Create("Soy un héroe.", "I am a hero."),
            Tuple.Create("Las personas me admiran.", "People admire me."),
            Tuple.Create("Merezco triunfar.", "I deserve to succeed."),
            Tuple.Create("Estoy tranquilo.", "I am calm."),
            Tuple.Create("Mantengo mi confianza en todo momento y lugar.", "I maintain my self-confidence in all times and places."),
            Tuple.Create("Me perdono a mí mismo.", "I forgive myself."),
            Tuple.Create("Tengo paz interior.", "I have inner peace."),
            Tuple.Create("Sano todos los problemas que afectan mi confianza.", "I heal all issues affecting my self-confidence."),
            Tuple.Create("Soy inteligente.", "I am intelligent."),
            Tuple.Create("Me perdono todos los errores pasados.", "I forgive myself for any and all past mistakes."),
            Tuple.Create("Estoy seguro de mí mismo.", "I am secure in myself."),
            Tuple.Create("Confío en mí mismo.", "I am confident in myself."),
            Tuple.Create("Descubro nuevos aspectos de mi confianza cada día.", "I discover new aspects of my self-confidence daily."),
            Tuple.Create("Actúo.", "I take action."),
            Tuple.Create("Equilibro perfectamente mi confianza con la modestia.", "I balance my self-confidence with modesty perfectly."),
            Tuple.Create("Reconozco y honro mis talentos, capacidades y habilidades.", "I recognize and honor my talents, abilities, and skills."),
            Tuple.Create("Veo cada parte de mi vida como una lección.", "I see each part of my life as a lesson."),
            Tuple.Create("Me siento seguro de adentro hacia afuera.", "I feel confident from the inside out.")
        };

        private static readonly Tuple<string, string>[] WeightLoss =
        {
            Tuple.Create("Mi cuerpo es perfecto ahora mismo.", "My body is perfect right now."),
            Tuple.Create("A medida que cambio mis pensamientos, mi cuerpo cambia.", "As I change my thoughts, my body changes."),
            Tuple.Create("Soy más que mi cuerpo o mi cerebro.", "I am more than my body or my brain."),
            Tuple.Create("Logro mis metas de pérdida de peso.", "I achieve my weight loss goals."),
            Tuple.Create("Elijo alimentos nutritivos y saludables.", "I choose nourishing, healthy foods."),
            Tuple.Create("Pienso antes de comer.", "I think before eating."),
            Tuple.Create("Bebo mucha agua.", "I drink lots of water."),
            Tuple.Create("Los alimentos saludables saben mejor.", "Healthy foods taste better."),
            Tuple.Create("Me motivan tanto los éxitos como los fracasos.", "I am motivated by both successes and failures."),
            Tuple.Create("Acepto y amo mi cuerpo tal como es, y trabajo para mejorarlo.", "I accept and love my body as it is, and work to make it better."),
            Tuple.Create("Amo los desafíos y los abrazo.", "I love challenges and embrace them."),
            Tuple.Create("Pierdo peso de forma sistemática y lo mantengo permanentemente.", "I lose weight systematically and I keep it off permanently."),
            Tuple.Create("Estoy perdiendo peso.", "I am losing weight."),
            Tuple.Create("Hago ejercicio porque me hace sentir bien.", "I exercise because it makes me feel good."),
            Tuple.Create("Respeto mi cuerpo y lo trato con respeto.", "I respect my body and treat it with respect."),
            Tuple.Create("Hago todo lo necesario para alcanzar mi peso saludable.", "I do everything I need to do to achieve my healthy weight."),
            Tuple.Create("Cada éxito me anima.", "I am encouraged by every success."),
            Tuple.Create("Cada tropiezo me motiva.", "I am motivated by every shortfall."),
            Tuple.Create("Disuelvo todos los bloqueos para alcanzar un peso saludable.", "I dissolve all blocks to reaching a healthy weight."),
            Tuple.Create("Me perdono a mí mismo.", "I forgive myself."),
            Tuple.Create("Aprendo de mis errores.", "I learn from my mistakes."),
            Tuple.Create("Satisfago todos mis apetitos físicos de formas saludables.", "I fill all physical appetites in physically healthy ways."),
            Tuple.Create("Soy consciente de mis hábitos alimenticios y de cómo afectan mi peso.", "I am aware of my eating habits and how they affect my weight."),
            Tuple.Create("Estoy dispuesto a cambiar mis hábitos alimenticios y lo hago con facilidad.", "I am willing to change my eating habits, and I do so easily."),
            Tuple.Create("Desarrollo masa muscular magra.", "I build lean muscle."),
            Tuple.Create("Disfruto el proceso de alcanzar un peso saludable.", "I enjoy the process of reaching a healthy weight."),
            Tuple.Create("Me veo en mi peso saludable y lo logro.", "I see myself at my healthy weight and I achieve it."),
            Tuple.Create("Tengo determinación diaria constante para alcanzar mi peso saludable.", "I have non-stop daily determination to reach my healthy weight."),
            Tuple.Create("Me gustan los paseos largos.", "I like long walks."),
            Tuple.Create("Me resulta fácil mantener mi plan para alcanzar mi peso saludable.", "It is easy for me to stay on my plan to obtain my healthy weight."),
            Tuple.Create("Me imagino en mi peso perfecto.", "I picture myself at my perfect weight."),
            Tuple.Create("Tengo una actitud positiva sobre qué como, cómo como y cuándo como.", "I have a positive attitude about what I eat, how I eat, and when I eat."),
            Tuple.Create("Desarrollar hábitos alimenticios saludables es cada día más fácil.", "Developing healthy eating habits becomes easier each day."),
            Tuple.Create("Sigo un plan de alimentación saludable y mantengo mi peso saludable con facilidad.", "I stay on a healthy eating plan and maintain my healthy weight easily."),
            Tuple.Create("Cada día me vuelvo automáticamente más y más saludable.", "Each day I automatically and successfully get healthier and healthier."),
            Tuple.Create("Estoy sano y fuerte.", "I am healthy and strong."),
            Tuple.Create("Llevo una dieta bien equilibrada.", "I eat a well-balanced diet."),
            Tuple.Create("Disfruto comiendo alimentos deliciosos y saludables.", "I enjoy eating delicious and healthy food."),
            Tuple.Create("Mi cuerpo ama y merece alimentos fáciles de digerir y saludables.", "My body loves and deserves food that is easy to digest and healthy."),
            Tuple.Create("Hago ejercicio regularmente de forma relajada y agradable.", "I exercise regularly in a relaxed and enjoyable manner.")
        };

        private static Tuple<string, string>[] GetArray(AffirmationTheme theme)
        {
            switch (theme)
            {
                case AffirmationTheme.PositiveThinking: return PositiveThinking;
                case AffirmationTheme.Prosperity: return Prosperity;
                case AffirmationTheme.QuitBadHabits: return QuitBadHabits;
                case AffirmationTheme.SelfConfidence: return SelfConfidence;
                case AffirmationTheme.WeightLoss: return WeightLoss;
                default: return General;
            }
        }

        public static int ThemeCount(AffirmationTheme theme)
        {
            return GetArray(theme).Length;
        }

        public static int TotalCount()
        {
            return General.Length + PositiveThinking.Length + Prosperity.Length +
                   QuitBadHabits.Length + SelfConfidence.Length + WeightLoss.Length;
        }

        public static string Get(AffirmationTheme theme, int index)
        {
            Tuple<string, string>[] arr = GetArray(theme);
            if (index < 0) index = 0;
            if (index >= arr.Length) index = arr.Length - 1;
            return (Localization.CurrentLanguage == Language.Spanish) ? arr[index].Item1 : arr[index].Item2;
        }

        // Shuffle the complete bank so the Zen manager can rotate through
        // every mantra exactly once before repeating any phrase.
        public static List<Tuple<AffirmationTheme, int>> BuildOrder(Random rnd)
        {
            var order = new List<Tuple<AffirmationTheme, int>>();
            foreach (AffirmationTheme theme in Enum.GetValues(typeof(AffirmationTheme)))
            {
                for (int i = 0; i < ThemeCount(theme); i++)
                    order.Add(Tuple.Create(theme, i));
            }
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                var tmp = order[i];
                order[i] = order[j];
                order[j] = tmp;
            }
            return order;
        }
    }
}