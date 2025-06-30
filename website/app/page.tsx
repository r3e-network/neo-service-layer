import { HeroSection } from '@/components/home/HeroSection'
import { FeaturesSection } from '@/components/home/FeaturesSection'
import { ServicesSection } from '@/components/home/ServicesSection'
import { StatsSection } from '@/components/home/StatsSection'
import { CTASection } from '@/components/home/CTASection'
import { TestimonialsSection } from '@/components/home/TestimonialsSection'

export default function HomePage() {
  return (
    <div className="relative">
      <HeroSection />
      <StatsSection />
      <FeaturesSection />
      <ServicesSection />
      <TestimonialsSection />
      <CTASection />
    </div>
  )
}