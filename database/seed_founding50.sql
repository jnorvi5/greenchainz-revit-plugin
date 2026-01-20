-- =====================================================
-- GreenChainz Database Seed Script
-- Run this in Supabase SQL Editor
-- =====================================================

-- Step 1: Add Geolocation columns to Suppliers
ALTER TABLE public.suppliers 
ADD COLUMN IF NOT EXISTS latitude DECIMAL(9,6),
ADD COLUMN IF NOT EXISTS longitude DECIMAL(9,6),
ADD COLUMN IF NOT EXISTS city TEXT,
ADD COLUMN IF NOT EXISTS state TEXT,
ADD COLUMN IF NOT EXISTS service_radius_miles INTEGER DEFAULT 250;

-- Step 2: Add Carbon & Logistics columns to Products
ALTER TABLE public.products 
ADD COLUMN IF NOT EXISTS gwp_per_unit DECIMAL(10,4),
ADD COLUMN IF NOT EXISTS unit_of_measure TEXT DEFAULT 'm3',
ADD COLUMN IF NOT EXISTS kg_per_unit DECIMAL(10,2),
ADD COLUMN IF NOT EXISTS has_epd BOOLEAN DEFAULT false,
ADD COLUMN IF NOT EXISTS epd_url TEXT,
ADD COLUMN IF NOT EXISTS certifications TEXT[];

-- =====================================================
-- SEED: Founding 50 Suppliers
-- =====================================================

-- CONCRETE SUPPLIERS
INSERT INTO public.suppliers (name, latitude, longitude, city, state, service_radius_miles, sustainability_score, contact_email, website)
VALUES 
  ('CarbonCure Technologies - Charlotte', 35.2271, -80.8431, 'Charlotte', 'NC', 200, 96, 'sales@carboncure.com', 'https://carboncure.com'),
  ('CarbonCure Technologies - Denver', 39.7392, -104.9903, 'Denver', 'CO', 250, 96, 'sales@carboncure.com', 'https://carboncure.com'),
  ('CarbonCure Technologies - Bay Area', 37.7749, -122.4194, 'San Francisco', 'CA', 150, 96, 'sales@carboncure.com', 'https://carboncure.com'),
  ('Solidia Technologies', 40.4774, -74.2591, 'Piscataway', 'NJ', 300, 94, 'info@solidiatech.com', 'https://solidiatech.com'),
  ('Central Concrete - San Jose', 37.3382, -121.8863, 'San Jose', 'CA', 100, 88, 'orders@centralconcrete.com', 'https://centralconcrete.com'),
  ('Holcim US - Chicago', 41.8781, -87.6298, 'Chicago', 'IL', 200, 85, 'info@holcim.us', 'https://holcim.us'),
  ('Lehigh Hanson - Atlanta', 33.7490, -84.3880, 'Atlanta', 'GA', 200, 82, 'info@lehighhanson.com', 'https://lehighhanson.com'),
  ('CEMEX - Houston', 29.7604, -95.3698, 'Houston', 'TX', 300, 80, 'info@cemexusa.com', 'https://cemexusa.com')
ON CONFLICT DO NOTHING;

-- STEEL SUPPLIERS
INSERT INTO public.suppliers (name, latitude, longitude, city, state, service_radius_miles, sustainability_score, contact_email, website)
VALUES 
  ('Nucor Corporation - Birmingham', 33.5207, -86.8025, 'Birmingham', 'AL', 400, 91, 'nucor@nucor.com', 'https://nucor.com'),
  ('Nucor Corporation - Charlotte', 35.2271, -80.8431, 'Charlotte', 'NC', 350, 91, 'nucor@nucor.com', 'https://nucor.com'),
  ('SSAB Americas - Mobile', 30.6954, -88.0399, 'Mobile', 'AL', 500, 95, 'info@ssab.com', 'https://ssab.com'),
  ('Commercial Metals Company', 32.7767, -96.7970, 'Dallas', 'TX', 400, 87, 'info@cmc.com', 'https://cmc.com'),
  ('Steel Dynamics - Fort Wayne', 41.0793, -85.1394, 'Fort Wayne', 'IN', 350, 86, 'info@steeldynamics.com', 'https://steeldynamics.com'),
  ('ArcelorMittal - Chicago', 41.8781, -87.6298, 'Chicago', 'IL', 400, 78, 'info@arcelormittal.com', 'https://arcelormittal.com')
ON CONFLICT DO NOTHING;

-- MASS TIMBER / WOOD SUPPLIERS
INSERT INTO public.suppliers (name, latitude, longitude, city, state, service_radius_miles, sustainability_score, contact_email, website)
VALUES 
  ('Structurlam - Oregon', 44.9429, -123.0351, 'Salem', 'OR', 500, 96, 'info@structurlam.com', 'https://structurlam.com'),
  ('Nordic Structures', 45.5017, -73.5673, 'Montreal', 'QC', 800, 94, 'info@nordic.ca', 'https://nordic.ca'),
  ('Katerra CLT', 37.3861, -122.0839, 'Mountain View', 'CA', 400, 92, 'info@katerra.com', 'https://katerra.com'),
  ('SmartLam - Montana', 46.8721, -114.0004, 'Missoula', 'MT', 600, 93, 'info@smartlam.com', 'https://smartlam.com'),
  ('Mercer Mass Timber', 45.5152, -122.6784, 'Portland', 'OR', 400, 91, 'info@mercermasstimber.com', 'https://mercermasstimber.com')
ON CONFLICT DO NOTHING;

-- INSULATION SUPPLIERS
INSERT INTO public.suppliers (name, latitude, longitude, city, state, service_radius_miles, sustainability_score, contact_email, website)
VALUES 
  ('Rockwool - Mississippi', 34.3668, -89.5197, 'Oxford', 'MS', 500, 92, 'info@rockwool.com', 'https://rockwool.com'),
  ('Owens Corning - Toledo', 41.6528, -83.5379, 'Toledo', 'OH', 400, 88, 'info@owenscorning.com', 'https://owenscorning.com'),
  ('Knauf Insulation - Indiana', 39.7684, -86.1581, 'Indianapolis', 'IN', 350, 86, 'info@knaufinsulation.com', 'https://knaufinsulation.com'),
  ('Johns Manville - Denver', 39.7392, -104.9903, 'Denver', 'CO', 400, 84, 'info@jm.com', 'https://jm.com')
ON CONFLICT DO NOTHING;

-- GLASS SUPPLIERS
INSERT INTO public.suppliers (name, latitude, longitude, city, state, service_radius_miles, sustainability_score, contact_email, website)
VALUES 
  ('Guardian Glass - Michigan', 42.2808, -83.7430, 'Auburn Hills', 'MI', 500, 87, 'info@guardian.com', 'https://guardianglass.com'),
  ('Vitro Architectural Glass', 40.4406, -79.9959, 'Pittsburgh', 'PA', 400, 85, 'info@vitroglazings.com', 'https://vitroglazings.com'),
  ('AGC Glass - Georgia', 33.7490, -84.3880, 'Atlanta', 'GA', 400, 83, 'info@agc.com', 'https://agc-glass.com')
ON CONFLICT DO NOTHING;

-- ALUMINUM SUPPLIERS
INSERT INTO public.suppliers (name, latitude, longitude, city, state, service_radius_miles, sustainability_score, contact_email, website)
VALUES 
  ('Novelis - Atlanta', 33.7490, -84.3880, 'Atlanta', 'GA', 500, 90, 'info@novelis.com', 'https://novelis.com'),
  ('Hydro Aluminum - Kentucky', 38.2527, -85.7585, 'Louisville', 'KY', 400, 88, 'info@hydro.com', 'https://hydro.com'),
  ('Alcoa - Pittsburgh', 40.4406, -79.9959, 'Pittsburgh', 'PA', 400, 82, 'info@alcoa.com', 'https://alcoa.com')
ON CONFLICT DO NOTHING;

-- =====================================================
-- SEED: Products with Carbon Data
-- =====================================================

-- Get supplier IDs and insert products
DO $$
DECLARE
  carboncure_charlotte_id UUID;
  carboncure_denver_id UUID;
  solidia_id UUID;
  nucor_birmingham_id UUID;
  ssab_id UUID;
  structurlam_id UUID;
  rockwool_id UUID;
  guardian_id UUID;
  novelis_id UUID;
BEGIN
  -- Get supplier IDs
  SELECT id INTO carboncure_charlotte_id FROM public.suppliers WHERE name LIKE '%CarbonCure%Charlotte%' LIMIT 1;
  SELECT id INTO carboncure_denver_id FROM public.suppliers WHERE name LIKE '%CarbonCure%Denver%' LIMIT 1;
  SELECT id INTO solidia_id FROM public.suppliers WHERE name LIKE '%Solidia%' LIMIT 1;
  SELECT id INTO nucor_birmingham_id FROM public.suppliers WHERE name LIKE '%Nucor%Birmingham%' LIMIT 1;
  SELECT id INTO ssab_id FROM public.suppliers WHERE name LIKE '%SSAB%' LIMIT 1;
  SELECT id INTO structurlam_id FROM public.suppliers WHERE name LIKE '%Structurlam%' LIMIT 1;
  SELECT id INTO rockwool_id FROM public.suppliers WHERE name LIKE '%Rockwool%' LIMIT 1;
  SELECT id INTO guardian_id FROM public.suppliers WHERE name LIKE '%Guardian%' LIMIT 1;
  SELECT id INTO novelis_id FROM public.suppliers WHERE name LIKE '%Novelis%' LIMIT 1;

  -- CONCRETE PRODUCTS
  IF carboncure_charlotte_id IS NOT NULL THEN
    INSERT INTO public.products (supplier_id, name, description, gwp_per_unit, kg_per_unit, unit_of_measure, has_epd, epd_url, certifications, is_verified)
    VALUES 
      (carboncure_charlotte_id, 'CarbonCure Ready-Mix 4000psi', 'CO2-injected ready-mix concrete with 30% lower carbon', 238, 2400, 'm3', true, 'https://carboncure.com/epd/4000psi', ARRAY['EPD', 'Carbon Negative Technology', 'LEED v4.1'], true),
      (carboncure_charlotte_id, 'CarbonCure Ready-Mix 5000psi', 'High-strength CO2-injected concrete', 265, 2450, 'm3', true, 'https://carboncure.com/epd/5000psi', ARRAY['EPD', 'Carbon Negative Technology'], true)
    ON CONFLICT DO NOTHING;
  END IF;

  IF solidia_id IS NOT NULL THEN
    INSERT INTO public.products (supplier_id, name, description, gwp_per_unit, kg_per_unit, unit_of_measure, has_epd, certifications, is_verified)
    VALUES 
      (solidia_id, 'Solidia Cement', 'Revolutionary low-carbon cement using CO2 curing', 180, 2300, 'm3', true, ARRAY['EPD', 'Cradle to Cradle', '70% Lower Carbon'], true)
    ON CONFLICT DO NOTHING;
  END IF;

  -- STEEL PRODUCTS
  IF nucor_birmingham_id IS NOT NULL THEN
    INSERT INTO public.products (supplier_id, name, description, gwp_per_unit, kg_per_unit, unit_of_measure, has_epd, certifications, is_verified)
    VALUES 
      (nucor_birmingham_id, 'Nucor EAF Structural Steel', 'Electric arc furnace steel with 93% recycled content', 690, 7850, 'ton', true, ARRAY['EPD', 'ISO 14001', '93% Recycled Content'], true),
      (nucor_birmingham_id, 'Nucor Recycled Rebar #4-#11', 'Recycled steel rebar for reinforcement', 620, 7850, 'ton', true, ARRAY['EPD', '97% Recycled'], true)
    ON CONFLICT DO NOTHING;
  END IF;

  IF ssab_id IS NOT NULL THEN
    INSERT INTO public.products (supplier_id, name, description, gwp_per_unit, kg_per_unit, unit_of_measure, has_epd, certifications, is_verified)
    VALUES 
      (ssab_id, 'SSAB Fossil-Free Steel', 'Zero fossil fuel steel using HYBRIT technology', 50, 7850, 'ton', true, ARRAY['EPD', 'HYBRIT', 'Zero Fossil', 'Science Based Target'], true)
    ON CONFLICT DO NOTHING;
  END IF;

  -- MASS TIMBER PRODUCTS
  IF structurlam_id IS NOT NULL THEN
    INSERT INTO public.products (supplier_id, name, description, gwp_per_unit, kg_per_unit, unit_of_measure, has_epd, certifications, is_verified)
    VALUES 
      (structurlam_id, 'Structurlam CrossLam CLT', 'Cross-laminated timber panels - carbon negative', -500, 500, 'm3', true, ARRAY['FSC', 'EPD', 'Carbon Negative', 'PEFC'], true),
      (structurlam_id, 'Structurlam Glulam Beams', 'Glue-laminated timber structural beams', -400, 480, 'm3', true, ARRAY['FSC', 'EPD', 'Carbon Storing'], true)
    ON CONFLICT DO NOTHING;
  END IF;

  -- INSULATION PRODUCTS
  IF rockwool_id IS NOT NULL THEN
    INSERT INTO public.products (supplier_id, name, description, gwp_per_unit, kg_per_unit, unit_of_measure, has_epd, certifications, is_verified)
    VALUES 
      (rockwool_id, 'Rockwool ComfortBatt', 'Stone wool batt insulation R-15', 28, 30, 'm3', true, ARRAY['EPD', 'GREENGUARD Gold', 'Cradle to Cradle Silver'], true),
      (rockwool_id, 'Rockwool Safe n Sound', 'Acoustic stone wool insulation', 32, 35, 'm3', true, ARRAY['EPD', 'GREENGUARD'], true)
    ON CONFLICT DO NOTHING;
  END IF;

  -- GLASS PRODUCTS
  IF guardian_id IS NOT NULL THEN
    INSERT INTO public.products (supplier_id, name, description, gwp_per_unit, kg_per_unit, unit_of_measure, has_epd, certifications, is_verified)
    VALUES 
      (guardian_id, 'Guardian SunGuard SNX 62/27', 'High-performance solar control glass', 1150, 2500, 'm2', true, ARRAY['EPD', '30% Recycled Cullet', 'LEED'], true)
    ON CONFLICT DO NOTHING;
  END IF;

  -- ALUMINUM PRODUCTS
  IF novelis_id IS NOT NULL THEN
    INSERT INTO public.products (supplier_id, name, description, gwp_per_unit, kg_per_unit, unit_of_measure, has_epd, certifications, is_verified)
    VALUES 
      (novelis_id, 'Novelis evercan Aluminum', 'High recycled content architectural aluminum', 2000, 2700, 'ton', true, ARRAY['EPD', 'ASI', '82% Recycled Content'], true)
    ON CONFLICT DO NOTHING;
  END IF;

END $$;

-- =====================================================
-- Verify Data
-- =====================================================
SELECT 'Suppliers' as table_name, COUNT(*) as count FROM public.suppliers WHERE latitude IS NOT NULL
UNION ALL
SELECT 'Products with GWP', COUNT(*) FROM public.products WHERE gwp_per_unit IS NOT NULL
UNION ALL
SELECT 'Products with EPD', COUNT(*) FROM public.products WHERE has_epd = true;
