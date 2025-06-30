'use client'

import { StarIcon } from '@heroicons/react/24/solid'
import { ChatBubbleLeftIcon as QuoteIcon } from '@heroicons/react/24/outline'
import { AnimatedDiv } from '@/components/ui/AnimatedDiv'

const testimonials = [
  {
    id: 1,
    content: "Neo Service Layer transformed our development process. What used to take months now takes weeks. The pre-built services are production-ready and incredibly well-documented.",
    author: {
      name: "Sarah Chen",
      role: "Lead Developer",
      company: "DeFi Protocol",
      avatar: "https://images.unsplash.com/photo-1494790108755-2616b612b786?w=64&h=64&fit=crop&crop=face"
    },
    rating: 5,
    category: "DeFi"
  },
  {
    id: 2,
    content: "The cross-chain functionality and oracle integration saved us months of development time. The playground is perfect for testing before deployment.",
    author: {
      name: "Marcus Johnson",
      role: "CTO",
      company: "Web3 Startup",
      avatar: "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=64&h=64&fit=crop&crop=face"
    },
    rating: 5,
    category: "Infrastructure"
  },
  {
    id: 3,
    content: "As a solo developer, Neo Service Layer gives me the power of a full team. The modular architecture means I can build complex dApps without reinventing the wheel.",
    author: {
      name: "Elena Rodriguez",
      role: "Indie Developer",
      company: "GameFi Studio",
      avatar: "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=64&h=64&fit=crop&crop=face"
    },
    rating: 5,
    category: "Gaming"
  },
  {
    id: 4,
    content: "The security features and audit trails give our enterprise clients confidence. The compliance contract handles everything we need for regulatory requirements.",
    author: {
      name: "David Kim",
      role: "Blockchain Architect",
      company: "Enterprise Solutions",
      avatar: "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=64&h=64&fit=crop&crop=face"
    },
    rating: 5,
    category: "Enterprise"
  },
  {
    id: 5,
    content: "The analytics and monitoring tools provide insights we never had before. Real-time dashboards help us optimize our dApp performance continuously.",
    author: {
      name: "Lisa Wang",
      role: "Product Manager",
      company: "Analytics Platform",
      avatar: "https://images.unsplash.com/photo-1517841905240-472988babdf9?w=64&h=64&fit=crop&crop=face"
    },
    rating: 5,
    category: "Analytics"
  },
  {
    id: 6,
    content: "The healthcare services handle patient data privacy perfectly. HIPAA compliance built-in means we can focus on our core medical algorithms.",
    author: {
      name: "Dr. James Miller",
      role: "Chief Medical Officer",
      company: "HealthTech Innovators",
      avatar: "https://images.unsplash.com/photo-1612349317150-e413f6a5b16d?w=64&h=64&fit=crop&crop=face"
    },
    rating: 5,
    category: "Healthcare"
  }
]

const categories = [
  { name: 'DeFi', color: 'text-neo-500', bgColor: 'bg-neo-500/10' },
  { name: 'Infrastructure', color: 'text-blue-500', bgColor: 'bg-blue-500/10' },
  { name: 'Gaming', color: 'text-purple-500', bgColor: 'bg-purple-500/10' },
  { name: 'Enterprise', color: 'text-orange-500', bgColor: 'bg-orange-500/10' },
  { name: 'Analytics', color: 'text-pink-500', bgColor: 'bg-pink-500/10' },
  { name: 'Healthcare', color: 'text-green-500', bgColor: 'bg-green-500/10' },
]

function TestimonialCard({ testimonial, index }: { testimonial: typeof testimonials[0], index: number }) {
  const category = categories.find(cat => cat.name === testimonial.category)
  
  return (
    <AnimatedDiv
      direction="up"
      duration={600}
      delay={index * 100}
      className="group relative"
    >
      {/* Background glow */}
      <div className="absolute -inset-0.5 bg-gradient-to-r from-neo-500/20 to-services-business/20 rounded-lg blur opacity-0 group-hover:opacity-100 transition duration-300" />
      
      {/* Card content */}
      <div className="relative glass rounded-lg p-6 hover:border-neo-500/30 transition-all duration-300 h-full">
        {/* Rating stars */}
        <div className="flex items-center space-x-1 mb-4">
          {[...Array(testimonial.rating)].map((_, i) => (
            <StarIcon key={i} className="h-4 w-4 text-yellow-500" />
          ))}
        </div>

        {/* Quote icon */}
        <QuoteIcon className="h-8 w-8 text-neo-500/30 mb-4" />

        {/* Testimonial content */}
        <blockquote className="text-gray-300 text-sm leading-relaxed mb-6">
          "{testimonial.content}"
        </blockquote>

        {/* Author info */}
        <div className="flex items-center space-x-3">
          <div className="flex-shrink-0">
            <div className="w-10 h-10 rounded-full bg-gradient-to-r from-neo-500 to-services-business flex items-center justify-center text-white font-semibold text-sm">
              {testimonial.author.name.split(' ').map(n => n[0]).join('')}
            </div>
          </div>
          <div className="flex-1 min-w-0">
            <div className="text-white font-medium text-sm">
              {testimonial.author.name}
            </div>
            <div className="text-gray-400 text-xs">
              {testimonial.author.role}
            </div>
            <div className="text-gray-500 text-xs">
              {testimonial.author.company}
            </div>
          </div>
          {category && (
            <div className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${category.color} ${category.bgColor}`}>
              {category.name}
            </div>
          )}
        </div>
      </div>
    </AnimatedDiv>
  )
}

export function TestimonialsSection() {
  return (
    <section className="relative py-20 sm:py-32">
      {/* Background decoration */}
      <div className="absolute inset-0 overflow-hidden">
        <div className="absolute top-0 right-1/4 w-72 h-72 bg-services-business/5 rounded-full blur-3xl" />
        <div className="absolute bottom-0 left-1/4 w-72 h-72 bg-neo-500/5 rounded-full blur-3xl" />
      </div>

      <div className="relative mx-auto max-w-7xl px-6 lg:px-8">
        {/* Section header */}
        <AnimatedDiv direction="up" duration={600} className="text-center mb-16">
          <h2 className="text-3xl font-bold tracking-tight text-white sm:text-4xl lg:text-5xl">
            Trusted by{' '}
            <span className="text-gradient">developers</span> worldwide
          </h2>
          <p className="mt-6 text-lg leading-8 text-gray-400 max-w-3xl mx-auto">
            From indie developers to enterprise teams, see how Neo Service Layer
            is helping build the next generation of decentralized applications.
          </p>
        </AnimatedDiv>

        {/* Category filters */}
        <AnimatedDiv
          direction="up"
          duration={600}
          delay={200}
          className="flex flex-wrap justify-center gap-3 mb-12"
        >
          {categories.map((category, index) => (
            <div
              key={category.name}
              className={`inline-flex items-center px-3 py-2 rounded-full text-sm font-medium ${category.color} ${category.bgColor} border border-transparent hover:border-current/20 transition-all duration-200`}
            >
              {category.name}
            </div>
          ))}
        </AnimatedDiv>

        {/* Testimonials grid */}
        <div className="grid grid-cols-1 gap-8 md:grid-cols-2 lg:grid-cols-3">
          {testimonials.map((testimonial, index) => (
            <TestimonialCard
              key={testimonial.id}
              testimonial={testimonial}
              index={index}
            />
          ))}
        </div>

        {/* Bottom stats */}
        <AnimatedDiv
          direction="up"
          duration={600}
          delay={600}
          className="mt-16 text-center"
        >
          <div className="grid grid-cols-2 gap-8 md:grid-cols-4 max-w-3xl mx-auto">
            <div className="text-center">
              <div className="text-2xl font-bold text-white">500+</div>
              <div className="text-sm text-gray-400">Happy Developers</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-white">1000+</div>
              <div className="text-sm text-gray-400">Projects Built</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-white">99.9%</div>
              <div className="text-sm text-gray-400">Uptime</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-white">4.9★</div>
              <div className="text-sm text-gray-400">Average Rating</div>
            </div>
          </div>
        </AnimatedDiv>
      </div>
    </section>
  )
}